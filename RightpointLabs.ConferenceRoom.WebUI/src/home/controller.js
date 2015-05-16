(function() {
    'use strict;'

    angular.module('app').controller('HomeController', ['localStorageService', '$state', function(localStorageService, $state) {
        var defaultRoom = localStorageService.get('defaultRoom');
        if(defaultRoom) {
            $state.go('room', { roomAddress: defaultRoom });
        } else{
            $state.go('roomList');
        }

    }]);
})();