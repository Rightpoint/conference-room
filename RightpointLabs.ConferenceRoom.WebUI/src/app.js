(function() {
    'use strict;'

    angular.module('app', ['ng', 'restangular', 'ui.router', 'LocalStorageModule']).config(['RestangularProvider', '$urlRouterProvider', 'localStorageServiceProvider', function(RestangularProvider, $urlRouterProvider, localStorageServiceProvider) {
        RestangularProvider.setBaseUrl('/api');
        $urlRouterProvider.otherwise('/');
        localStorageServiceProvider.setStorageType('localStorage');
        localStorageServiceProvider.setPrefix('confRoom');
    }])
})();