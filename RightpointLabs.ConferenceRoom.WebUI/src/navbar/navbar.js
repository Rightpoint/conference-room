(function() {
    'use strict;'

    angular.module('app').directive('navbar', [function() {
        return {
            restrict: 'A',
            templateUrl: 'navbar/navbar.html',
            replace: true
        };
    }])
})();