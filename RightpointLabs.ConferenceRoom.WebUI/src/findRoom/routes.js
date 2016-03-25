(function() {
    'use strict;'

    angular.module('app').config(['$stateProvider', function($stateProvider) {
        $stateProvider
            .state('findRoom', {
                url: '/findRoom',
                templateUrl: 'findRoom/findRoom.html',
                controller: 'FindRoomController',
                controllerAs: 'c'
            });
    }])
})();