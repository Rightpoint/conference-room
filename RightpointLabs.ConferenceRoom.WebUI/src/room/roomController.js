(function() {
    'use strict;'

    angular.module('app').controller('RoomController', ['Restangular', '$timeout', '$interval', '$q', '$scope', 'matchmedia', 'UpdateHub', 'timelineService', '$state', 'soundService', 'timeService', '$stateParams', function(Restangular, $timeout, $interval, $q, $scope, matchmedia, UpdateHub, timelineService, $state, soundService, timeService, $stateParams) {
        var self = this;
        self.isLoading = 0;
        self.meetNowTime = 30;

        self.Free = 0;
        self.Busy = 1;
        self.BusyNotConfirmed = 2;

        self.Requested = 1;
        self.Denied = 2;
        self.Granted = 3;

        self.moment = window.moment;
        self.displayName = 'Loading...';
        self.roomAddress = $stateParams.roomAddress;
        var room = Restangular.one('room', self.roomAddress);

        self.currentTime = function currentTime() {
            return timeService.now().startOf('minute');
        };

        self.minutesUntil = function (value) {
            return moment(value).diff(self.currentTime(), 'minutes');
        }

        self.freeMinutes = function() {
            if(!self.current) {
                return null;
            }
            if (self.current.IsStarted) {
                return 0;
            }
            return self.minutesUntil(self.current.Start);
        };
        
        var early = 10;
        
        self.formatHour = function formatHour(time) {
            return moment(time).format('h');
        };
        
        self.formatTime = function formatTime(time) {
            return moment(time).format('h:mm a');
        };
        
        self.canManageCurrent = function canManageCurrent() {
            return self.canControl && self.current && !self.current.IsNotManaged;
        };
        
        self.showMeetNow = function showMeetNow() {
            return self.canControl && (!self.current || self.freeMinutes() > 10);
        }
        
        self.isCurrentInFuture = function isCurrentInFuture() {
            return self.current && self.minutesUntil(self.current.Start) > 0;
        };
        
        self.showStartEarly = function showStartEarly() {
            return self.canControl && self.canManageCurrent() && !self.current.IsStarted && self.freeMinutes() <= early;
        };
        
        self.showStart = function showStart() {
            return self.canControl && self.canManageCurrent() && !self.current.IsStarted && self.freeMinutes() <= 0;
        };
        
        self.showEndEarly = function showEndEarly() {
            return self.canControl && self.canManageCurrent() && self.current.IsStarted;
        };
        
        self.showEndAndStartNext = function showEndAndStartNext() {
            return self.canControl && self.showEndEarly() && self.next && !self.next.IsNotManaged && self.minutesUntil(self.next.Start) <= early;
        };
        
        self.isCurrentToday = function isCurrentToday() {
            return self.current && moment(self.current.Start).diff(self.currentTime().startOf('day').add(1, 'day'), 'minutes') < 0;
        };
        
        self.isNextToday = function isNextToday() {
            return self.next && moment(self.next.Start).diff(self.currentTime().startOf('day').add(1, 'day'), 'minutes') < 0;
        };
        
        self.canManagePrev = function canManagePrev() {
            return self.canControl && self.prev && !self.prev.IsNotManaged;
        };
        
        self.showMessagePrev = function showMessagePrev() {
            return self.canControl && self.canManagePrev() && self.minutesUntil(self.prev.End) > -10 && self.minutesUntil(self.prev.End) <= 0;
        };
        
        self.showAddNew = function showAddNew() {
            return self.canControl && (!self.current || self.freeMinutes() > early);
        };


        
        var infoTimeout = null;
        function loadInfo() {
            if(infoTimeout) {
                $timeout.cancel(infoTimeout);
            }
            return $q.when(room.one('info').get({ })).then(function(data) {
                if (!data || data.error) {
                    $state.go('settings');
                    return;
                }
                self.canControl = data.CanControl;

                timeDelta = timeService.setCurrentTime(data.CurrentTime);
                self.displayName = data.DisplayName;
                self.hasControllableDoor = self.canControl && data.HasControllableDoor;
                if(self.canControl) {
                    scheduleCancel();
                }
            }, function() {
                if(infoTimeout) {
                    $timeout.cancel(infoTimeout);
                }
                infoTimeout = $timeout(loadInfo, 60 * 1000); // if load failed, try again in 60 seconds
            });
        }

        var statusTimeout = null;
        var cancelTimeout = null;
        var warnings = {};
        var cancels = {};

        function scheduleCancel() {
            var current = self.current;
            if(cancelTimeout) {
                $timeout.cancel(cancelTimeout);
            }
            if(!self.canControl) {
                return; // can't control this room
            }
            if ($state.current.name != 'room') {
                return; // already left this page - orphaned event
            }
            if(!current || current.IsStarted) {
                return;
            }
            if (self.displayName === 'Loading...') {
                return; // not done loading yet
            } 
            if(current.IsNotManaged) {
                return; // unmanaged meeting
            }
            if(cancels[current.UniqueId]) {
                return; // already cancelled, we just aren't updated yet
            }

            var now = self.currentTime();
            var warnTime = warnings[current.UniqueId];
            if(!warnTime) {
                // we haven't warned yet - figure out when we should
                warnTime = moment(current.Start).add(5, 'minute');
                if(!warnTime.isAfter(now)) {
                    // whoops, we should have done that already.... let's do it now.
                    room.one('meeting').post('warnAbandon', {}, { uniqueId: current.UniqueId }).then(function () {
                        // ok, we've told people, just remember what time it is so we give them a minute to start the meeting
                        warnings[current.UniqueId] = self.currentTime();
                        if(cancelTimeout) {
                            $timeout.cancel(cancelTimeout);
                        }
                        cancelTimeout = $timeout(scheduleCancel, 61 * 1000);
                        soundService.play('resources/warn.mp3');
                    }, function() {
                        // well, the warning failed... maybe the item was deleted?  In any case, reloading the status will re-run us
                        loadStatus();
                    });
                } else {
                    // ok, not time to warn yet - re-run once it's time
                    cancelTimeout = $timeout(scheduleCancel, warnTime.diff(now, 'millisecond', true) + 1000);
                }
                return;
            }

            // ok, we've warned.  Have they had 2 minutes to get back to us yet?
            var canCancelAt = warnTime.clone().add(2, 'minute');
            if(!canCancelAt.isAfter(now)) {
                // they've taken too long - cancel it now
                room.one('meeting').post('abandon', {}, { uniqueId: current.UniqueId }).then(function() {
                    // ok, meeting is cancelled.  Just refresh
                    cancels[current.UniqueId] = self.currentTime();
                    loadStatus();
                    soundService.play('resources/cancel.mp3');
                }, function() {
                    // well, the cancel failed... maybe the item was deleted?  In any case, reloading the status will re-run us
                    loadStatus();
                });
            } else {
                // we need to give them some more time
                cancelTimeout = $timeout(scheduleCancel, canCancelAt.diff(now, 'millisecond', true) + 1000);
            }
        }

        function loadStatus() {
            if(statusTimeout) {
                $timeout.cancel(statusTimeout);
            }

            return room.one('status').get().then(function(data) {
                if(!data || data.error) {
                    self.doorStatus = null;
                    self.status = {};
                    self.appointments = [];
                    self.current = null;
                    self.next = null;
                    self.prev = null;
                    return;
                }

                self.doorStatus = data.DoorStatus;
                self.status = data.Status;
                self.appointments = _.sortBy(data.NearTermMeetings, 'Start');
                self.current = data.CurrentMeeting;
                self.next = data.NextMeeting;
                self.prev = data.PreviousMeeting;
                scheduleCancel();

                var waitTime = data.NextChangeSeconds ? Math.min(5 * 60, data.NextChangeSeconds + 1) : (5 * 60);
                if(statusTimeout) {
                    $timeout.cancel(statusTimeout);
                }
                statusTimeout = $timeout(loadStatus, waitTime * 1000);
            }, function() {
                if(statusTimeout) {
                    $timeout.cancel(statusTimeout);
                }
                statusTimeout = $timeout(loadStatus, 60 * 1000);
            });
        }

        self.start = function(item) {
            var p = room.one('meeting').post('start', {}, { uniqueId: item.UniqueId }).then(function() {
                soundService.play('resources/start.mp3');
                return loadStatus();
            });
            showIndicator(p);
        };
        self.cancel = function(item) {
            var p = room.one('meeting').post('abandon', {}, { uniqueId: item.UniqueId }).then(function() {
                soundService.play('resources/cancel.mp3');
                return loadStatus();
            });
            showIndicator(p);
        };
        self.end = function(item) {
            var p = room.one('meeting').post('end', {}, { uniqueId: item.UniqueId }).then(function() {
                soundService.play('resources/end.mp3');
                return loadStatus();
            });
            showIndicator(p);
        };
        self.endAndStartNext = function(item, next) {
            var p = room.one('meeting').post('end', {}, { uniqueId: item.UniqueId }).then(function() {
                soundService.play('resources/end.mp3');
                return room.one('meeting').post('start', {}, { uniqueId: next.UniqueId }).then(function() {
                    soundService.play('resources/start.mp3');
                    return loadStatus();
                });
            });
            showIndicator(p);
        };
        self.message = function(item) {
            var p = room.one('meeting').post('message', {}, { uniqueId: item.UniqueId });
            showIndicator(p);
        };
        self.refresh = function() {
            var p = $q.all([loadInfo(), loadStatus()]);
            showIndicator(p);
        };

        self.meetNow = function meetNow() {
            var p = room.one('meeting').post('startNew', {}, { title: 'Local Meeting', endTime: self.meetNowTime }).then(function() {
                soundService.play('resources/new.mp3');
                self.meetNowMinutes = 30;
                return loadStatus();
            });
            showIndicator(p);
        };

        self.openDoor = function openDoor() {
            var p = room.one('door').post('open', {}, { }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.closeDoor = function closeDoor() {
            var p = room.one('door').post('close', {}, { }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };

        function showIndicator(loadPromise) {
            self.isLoading++;
            loadPromise.finally(function() {
                self.isLoading--;
            });
        }

        self.refresh();

        var infoInterval = $interval(loadInfo, 60 * 60 * 1000);
        var scopeCycleInterval = $interval(function() {}, 10 * 1000);

        $scope.$on('roomRefresh', function(event, room) {
            if(self.roomAddress == room) {
                // don't trigger the spinners
                loadInfo();
                loadStatus();
            }
        });

        $scope.$on('$destroy', function() {
            if(infoInterval) {
                $interval.cancel(infoInterval);
            }
            if(infoTimeout) {
                $timeout.cancel(infoTimeout);
            }
            if(statusTimeout) {
                $timeout.cancel(statusTimeout);
            }
            if(cancelTimeout) {
                $timeout.cancel(cancelTimeout);
            }
            if(scopeCycleInterval) {
                $interval.cancel(scopeCycleInterval);
            }
        });

        // this might not be our default room... check...
        var redirectInterval = null;
        function resetInterval() {
            if(redirectInterval) {
               $interval.cancel(redirectInterval);
            }
            redirectInterval = $interval(function() {
                if(!self.canControl) {
                    $state.go('home');
                }
            }, 30000);
        }
        resetInterval();
        var resetEvents = 'mousedown mouseover mouseout mousemove';
        angular.element(document).on(resetEvents, resetInterval);
        $scope.$on('$destroy', function() {
            angular.element(document).off(resetEvents, resetInterval);
        });

    }]);
})();