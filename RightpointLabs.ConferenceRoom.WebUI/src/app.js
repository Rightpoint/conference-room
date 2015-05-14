(function() {
    'use strict;'

    angular.module('app', ['ng', 'restangular', 'ui.router']).config(['RestangularProvider', '$urlRouterProvider', function(RestangularProvider, $urlRouterProvider) {
        RestangularProvider.setBaseUrl('/api');
        $urlRouterProvider.otherwise('/');
    }])
})();