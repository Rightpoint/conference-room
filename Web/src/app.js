(function() {
    'use strict;'

    angular.module('app', ['ng', 'ngTouch', 'hmTouchEvents', 'restangular', 'ui.router', 'LocalStorageModule', 'matchmedia-ng', 'SignalR', 'ui.bootstrap', 'ngSanitize', 'ui.select']).config(['RestangularProvider', '$urlRouterProvider', 'localStorageServiceProvider', '$httpProvider', function(RestangularProvider, $urlRouterProvider, localStorageServiceProvider, $httpProvider) {
        RestangularProvider.setBaseUrl('/api');
        $urlRouterProvider.otherwise('/');
        localStorageServiceProvider.setStorageType('localStorage');
        localStorageServiceProvider.setPrefix('confRoom');

        $httpProvider.interceptors.push('requestInterceptor');
        $httpProvider.interceptors.push('authInterceptor');
    }]);
    
})();