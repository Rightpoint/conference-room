(function() {
    'use strict;'

    angular.module('app').config(['$stateProvider', function($stateProvider) {
        $stateProvider
            .state('buildingList', {
                url: '/buildingList',
                templateUrl: 'buildingList/buildingList.html',
                controller: 'BuildingListController',
                controllerAs: 'c'
            });
    }])
})();