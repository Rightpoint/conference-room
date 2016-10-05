(function() {
    'use strict;'

    angular.module('app').config(['$stateProvider', function($stateProvider) {
        $stateProvider
            .state('home', {
                url: '/',
                templateUrl: 'home/home.html',
                controller: 'HomeController',
                controllerAs: 'c'
            });
    }])
})();