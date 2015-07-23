(function() {
    'use strict;'

    angular.module('app').config(['$stateProvider', function($stateProvider) {
        $stateProvider
            .state('settings', {
                url: '/settings/',
                templateUrl: 'settings/settings.html',
                controller: 'SettingsController',
                controllerAs: 'c'
            });
    }])
})();