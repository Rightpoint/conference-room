(function() {
    'use strict;'

    angular.module('app').directive('header', [function() {
        return {
            restrict: 'E',
            templateUrl: 'directives/header/header.html',
            replace: true,
            scope: {
                title: '@'
            },
            link: function (scope, element, attr) {
            }
        };
    }])
})();