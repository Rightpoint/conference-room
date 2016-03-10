(function() {
    'use strict;'

    angular.module('app').controller('MeetingController', ['meeting', function(meeting) {
        var self = this;
        self.meeting = meeting;
        self.formatTime = function formatTime(value) {
            return moment(value).format('h:mm a');
        };
    }]);
})();