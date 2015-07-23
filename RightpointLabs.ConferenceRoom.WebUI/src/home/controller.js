(function() {
    'use strict;'

    angular.module('app').controller('HomeController', ['settings', '$state', function(settings, $state) {
        var defaultRoom = settings.defaultRoom;
        if(defaultRoom) {
            $state.go('room', { roomAddress: defaultRoom });
        } else{
            $state.go('roomList');
        }

    }]);
})();