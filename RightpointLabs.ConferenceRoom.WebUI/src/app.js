(function() {
    'use strict;'

    angular.module('app', ['ng', 'restangular', 'ui.router', 'LocalStorageModule']).config(['RestangularProvider', '$urlRouterProvider', 'localStorageServiceProvider', '$httpProvider', function(RestangularProvider, $urlRouterProvider, localStorageServiceProvider, $httpProvider) {
        RestangularProvider.setBaseUrl('/api');
        $urlRouterProvider.otherwise('/');
        localStorageServiceProvider.setStorageType('localStorage');
        localStorageServiceProvider.setPrefix('confRoom');

        $httpProvider.interceptors.push('requestInterceptor');
    }])
})();