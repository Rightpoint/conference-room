(function() {
    'use strict;'

    angular.module('app').config(['$stateProvider', function($stateProvider) {
        $stateProvider
            .state('roomList', {
                url: '/roomList',
                templateUrl: 'roomList/roomList.html',
                controller: 'RoomListController',
                controllerAs: 'c'
            })
            .state('roomList.details', {
                url: '/details/:roomListAddress',
                templateUrl: 'roomList/roomListDetails.html',
                controller: 'RoomListDetailsController',
                controllerAs: 'c'
            });
    }])
})();