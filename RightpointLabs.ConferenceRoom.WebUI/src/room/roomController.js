(function() {
    'use strict;'

    angular.module('app').controller('RoomController', ['Restangular', '$stateParams', '$timeout', '$interval', '$q', 'localStorageService', '$scope', 'matchmedia', function(Restangular, $stateParams, $timeout, $interval, $q, localStorageService, $scope, matchmedia) {
        var self = this;

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
        var securityKey = localStorageService.get('room_' + self.roomAddress) || '';
        self.isDefaultRoom = localStorageService.get('defaultRoom') == self.roomAddress;
        self.hasSecurityRights = false;

        var timeDelta = moment().diff(moment());
        self.currentTime = function currentTime() {
            return moment().add(-timeDelta, 'milliseconds').second(0).millisecond(0);
        };

        self.freeMinutes = function() {
            if(!self.current) {
                return null;
            }
            return self.moment(self.current.Start).diff(self.currentTime(), 'minutes');
        };

        function updateTimeline() {
            var now = self.currentTime();
            var day = now.clone().startOf('day');
            var start = day.clone().add(7, 'hours');
            var end = day.clone().add(18, 'hours');
            var totalMinutes = end.diff(start, 'minute', true);

            var marker = start;
            var markers = [];
            while(!marker.isAfter(end)) {
                markers.push({ position: marker.diff(start, 'minute', true) / totalMinutes, time: marker.isBefore(end) ? marker : null });
                marker = marker.clone().add(1, 'hour');
            }

            function buildTimelineRanges() {
                if(!self.appointments) {
                    return [{
                        size: 1,
                        isLoading: true
                    }];
                }
                var ranges = [];
                var current = start;
                _.each(self.appointments, function(item) {
                    var itemStart = moment(item.Start);
                    var itemEnd = moment(item.End);
                    if(!itemStart.isBefore(end) || !itemEnd.isAfter(current)){
                        return;
                    }
                    var freeTime = itemStart.diff(current, 'minute', true);
                    if(freeTime > 0) {
                        ranges.push({
                            size: freeTime/totalMinutes
                        });
                    }
                    var time = moment.min(itemEnd, end).diff(itemStart, 'minute', true);
                    if(time > 0) {
                        ranges.push({
                            size: time/totalMinutes,
                            appointment: item
                        })
                    }

                    current = itemEnd;
                });

                if(current.isBefore(end)) {
                    ranges.push({
                        size: end.diff(current, 'minute', true)/totalMinutes
                    });
                }

                return ranges;
            }

            self.timeline = {
                now: { position: Math.min(1.01, Math.max(-0.01, now.diff(start, 'minute', true) / totalMinutes)), time: now },
                markers: markers,
                ranges: buildTimelineRanges()
            };
        }

        var infoTimeout = null;
        function loadInfo() {
            if(infoTimeout) {
                $timeout.cancel(infoTimeout);
            }
            return room.one('info').get({ securityKey: securityKey }).then(function(data) {

                timeDelta = moment().diff(moment(data.CurrentTime));
                self.displayName = data.DisplayName;
                self.hasSecurityRights = data.SecurityStatus == 3; // granted
                updateTimeline();
                scheduleCancel();
            }, function() {
                infoTimeout = $timeout(loadInfo, 60 * 1000); // if load failed, try again in 60 seconds
            });
        }

        var statusTimeout = null;
        var cancelTimeout = null;
        var warnings = {};

        function scheduleCancel() {
            if(!self.hasSecurityRights) {
                return;
            }
            if(cancelTimeout) {
                $timeout.cancel(cancelTimeout);
            }
            if(!self.current || self.current.IsStarted) {
                return;
            }
            if(self.IsNotManaged) {
                return; // unmanaged meeting
            }

            var now = self.currentTime();
            var warnTime = warnings[self.current.UniqueId];
            if(!warnTime) {
                // we haven't warned yet - figure out when we should
                warnTime = moment(self.current.Start).add(4, 'minute');
                if(!warnTime.isAfter(now)) {
                    // whoops, we should have done that already.... let's do it now.
                    room.one('meeting').post('warnAbandon', {}, { securityKey: securityKey, uniqueId: self.current.UniqueId }).then(function() {
                        // ok, we've told people, just remember what time it is so we give them a minute to start the meeting
                        warnings[self.current.UniqueId] = self.currentTime();
                        if(cancelTimeout) {
                            $timeout.cancel(cancelTimeout);
                        }
                        cancelTimeout = $timeout(scheduleCancel, 61 * 1000);
                    }, function() {
                        // well, the warning failed... maybe the item was deleted?  In any case, reloading the status will re-run us
                        $timeout(loadStatus, 1000);
                    });
                } else {
                    // ok, not time to warn yet - re-run once it's time
                    cancelTimeout = $timeout(scheduleCancel, warnTime.diff(now, 'millisecond', true) + 1000);
                }
                return;
            }

            // ok, we've warned.  Have they had a minute to get back to us yet?
            var canCancelAt = warnTime.clone().add(1, 'minute');
            if(!canCancelAt.isAfter(now)) {
                // they've taken too long - cancel it now
                room.one('meeting').post('abandon', {}, { securityKey: securityKey, uniqueId: self.current.UniqueId }).then(function() {
                    // ok, meeting is cancelled.  Just refresh
                    loadStatus();
                }, function() {
                    // well, the cancel failed... maybe the item was deleted?  In any case, reloading the status will re-run us
                    $timeout(loadStatus, 1000);
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
                self.status = data.Status;
                self.appointments = _.sortBy(data.NearTermMeetings, 'Start');
                self.current = data.CurrentMeeting;
                self.next = data.NextMeeting;
                updateTimeline();
                scheduleCancel();

                var waitTime = data.NextChangeSeconds ? Math.min(5 * 60, data.NextChangeSeconds + 1) : (5 * 60);
                statusTimeout = $timeout(loadStatus, waitTime * 1000);
            }, function() {
                statusTimeout = $timeout(loadStatus, 60 * 1000);
            });
        }

        self.start = function(item) {
            var p = room.one('meeting').post('start', {}, { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.cancel = function(item) {
            var p = room.one('meeting').post('abandon', {}, { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.end = function(item) {
            var p = room.one('meeting').post('end', {}, { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.endAndStartNext = function(item, next) {
            var p = room.one('meeting').post('end', {}, { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return room.one('meeting').post('start', {}, { securityKey: securityKey, uniqueId: next.UniqueId }).then(function() {
                    return loadStatus();
                });
            });
            showIndicator(p);
        };
        self.requestControl = function() {
            if(!securityKey) {
                securityKey = (''+Math.random()).replace('.','');
                localStorageService.set('room_' + self.roomAddress, securityKey);
            }
            var p = room.post('requestAccess', {}, { securityKey: securityKey }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.setDefaultRoom = function() {
            localStorageService.set('defaultRoom', self.roomAddress);
            self.isDefaultRoom = localStorageService.get('defaultRoom') == self.roomAddress;
        };
        self.refresh = function() {
            var p = $q.all(loadInfo(), loadStatus());
            showIndicator(p);
        };

        function showIndicator(loadPromise) {

        }

        self.refresh();

        self.isTiny = false;
        var isTinyDispose = matchmedia.on('(max-width: 320px) and (max-height: 240px)', function(mql) {
            self.isTiny = mql.matches;
        });


        var infoInterval = $interval(loadInfo, 60 * 60 * 1000);
        var scopeCycleInterval = $interval(function() {}, 10 * 1000);

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
            isTinyDispose();
        });


    }]);
})();