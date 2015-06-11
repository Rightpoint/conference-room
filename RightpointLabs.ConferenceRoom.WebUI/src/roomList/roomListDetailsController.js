(function() {
    'use strict;'

    angular.module('app').controller('RoomListDetailsController', ['Restangular', '$stateParams', 'timelineService', function(Restangular, $stateParams, timelineService) {
        var self = this;
        self.roomLists = [];
        Restangular.one('roomList', $stateParams.roomListAddress).getList('rooms').then(function(data) {
            self.rooms = data;
            angular.forEach(self.rooms, function(room) {
                room.isLoading = true;
                return Restangular.one('room', room.Address).one('status').get().then(function(data) {
                    room.isLoading = false;
                    if(!data || data.error) {
                        room.status = {};
                        room.appointments = [];
                        room.current = null;
                        room.next = null;
                        return;
                    }

                    room.status = data.Status;
                    room.appointments = _.sortBy(data.NearTermMeetings, 'Start');
                    room.current = data.CurrentMeeting;
                    room.next = data.NextMeeting;

                    var moment = window.moment;
                    var now = moment();
                    var start = now.clone();
                    var end = now.clone().add(1, 'hours');

                    room.timeline = timelineService.build(start, end, room.appointments);
                }, function() {
                    room.isLoading = false;
                    room.status = null;
                });
            });
        });
    }]);
})();