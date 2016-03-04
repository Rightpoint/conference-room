(function() {
    'use strict;'

    angular.module('app').config(['$stateProvider', function($stateProvider) {
        $stateProvider
            .state('room', {
                url: '/',
                templateUrl: 'room/room.html',
                controller: 'RoomController',
                controllerAs: 'c'
            });
    }])
})();