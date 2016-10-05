(function() {
    'use strict;'

    angular.module('app').controller('RoomDetailController', ['room', 'statusService', function(room, statusService) {
        var self = this;
        self.room = room;
        self.status = statusService.statusText(room.CurrentMeeting, room.NearTermMeetings);
        self.statusClass = statusService.status(room.CurrentMeeting, room.NearTermMeetings).busy ? 'busy' : 'free';
    }]);
})();