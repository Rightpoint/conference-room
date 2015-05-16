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


        function loadInfo() {
            self.loadsInProgress++;
            return room.one('info').get({ securityKey: securityKey }).then(function(data) {
                self.loadsInProgress--;

                timeDelta = moment().diff(moment(data.CurrentTime));

                console.log(timeDelta, data.CurrentTime);
                console.log(moment().add(-timeDelta, 'milliseconds'));

                self.displayName = data.DisplayName;
            }, function() {
                self.loadsInProgress--;
            });
        }

        var statusTimeout = null;
        function loadStatus() {
            if(statusTimeout) {
                statusTimeout.cancel();
            }

            self.loadsInProgress++;
            return room.one('status').get().then(function(data) {
                self.loadsInProgress--;
                statusTimeout = $timeout(loadStatus, 60 * 1000);
                self.status = data.Status;
                self.appointments = _.sortBy(data.NearTermMeetings, 'Start');
                self.current = data.CurrentMeeting;
                self.next = data.NextMeeting;

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