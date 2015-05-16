(function() {
    'use strict;'

    angular.module('app').controller('RoomController', ['Restangular', '$stateParams', '$timeout', '$interval', '$q', function(Restangular, $stateParams, $timeout, $interval, $q) {
        var self = this;

        var securityKey = '';

        self.Free = 0;
        self.Busy = 1;
        self.BusyNotConfirmed = 2;

        self.moment = window.moment;
        self.loadsInProgress = 0;
        self.displayName = 'Loading...';
        self.roomAddress = $stateParams.roomAddress;
        var room = Restangular.one('room', self.roomAddress);

        var timeDelta = moment().diff(moment());
        self.currentTime = function currentTime() {
            return moment().add(-timeDelta, 'milliseconds');
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
                now: { position: Math.min(1.05, Math.max(-0.05, now.diff(start, 'minute', true) / totalMinutes)), time: now },
                markers: markers,
                ranges: buildTimelineRanges()
            };
        }

        function loadInfo() {
            self.loadsInProgress++;
            return room.one('info').get({ securityKey: securityKey }).then(function(data) {
                self.loadsInProgress--;

                timeDelta = moment().diff(moment(data.CurrentTime));
                self.displayName = data.DisplayName;
                updateTimeline();
            }, function() {
                self.loadsInProgress--;
            });
        }

        var statusTimeout = null;
        function loadStatus() {
            if(statusTimeout) {
                $timeout.cancel(statusTimeout);
            }

            self.loadsInProgress++;
            return room.one('status').get().then(function(data) {
                self.loadsInProgress--;
                statusTimeout = $timeout(loadStatus, 60 * 1000);
                self.status = data.Status;
                self.appointments = _.sortBy(data.NearTermMeetings, 'Start');
                self.current = data.CurrentMeeting;
                self.next = data.NextMeeting;
                updateTimeline();

                var waitTime = data.NextChangeSeconds ? Math.min(5 * 60, data.NextChangeSeconds + 1) : (5 * 60);
                statusTimeout = $timeout(loadStatus, waitTime * 1000);
            }, function() {
                self.loadsInProgress--;
                statusTimeout = $timeout(loadStatus, 60 * 1000);
            });
        }

        self.start = function(item) {
            var p = room.post('start', { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.cancel = function(item) {
            var p = room.post('abandon', { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.end = function(item) {
            var p = room.post('end', { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return loadStatus();
            });
            showIndicator(p);
        };
        self.endAndStartNext = function(item, next) {
            var p = room.post('end', { securityKey: securityKey, uniqueId: item.UniqueId }).then(function() {
                return room.post('start', { securityKey: securityKey, uniqueId: next.UniqueId }).then(function() {
                    return loadStatus();
                });
            });
            showIndicator(p);
        };
        self.refresh = function() {
            var p = $q.all(loadInfo(), loadStatus());
            showIndicator(p);
        };

        function showIndicator(loadPromise) {

        }

        self.refresh();

        $interval(loadInfo, 60 * 60 * 1000);


    }]);
})();