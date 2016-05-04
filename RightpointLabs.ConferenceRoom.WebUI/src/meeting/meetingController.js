(function() {
    'use strict;'

    angular.module('app').controller('MeetingController', ['meeting', function(meeting) {
        var self = this;
        self.meeting = meeting;
        
        var isSameDay = moment(meeting.Start).startOf('day').isSame(moment(meeting.End).startOf('day'));
        var format = isSameDay ? 'h:mm a' : 'MMMM Do YYYY, h:mm a';
        
        self.formatTime = function formatTime(value) {
            return moment(value).format(format);
        };
    }]);
})();