(function() {
    'use strict';

    angular.module('app').factory('spinner', ['$rootScope', function($rootScope){
        $rootScope.activeRequests = 0;

        return {
            activeRequests: function() {
                return $rootScope.activeRequests;
            },
            startRequest: function() {
                $rootScope.activeRequests++;
            },
            endRequest: function() {
                $rootScope.activeRequests--;
            }
        };
    }]);

    angular.module('app').directive('spinner', ['spinner', function(spinner){
        return {
            restrict: 'E',
            replace: true,
            scope: {},
            template: '<a class="spinner" ng-class="{ \'spinner-active\': !!spinner.activeRequests() }">{{ spinner.activeRequests() }}</a>',
            link: function(scope, element) {
                scope.spinner = spinner;
            }
        };
    }]);
})();