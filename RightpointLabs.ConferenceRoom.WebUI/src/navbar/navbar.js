(function() {
    'use strict;'

    angular.module('app').directive('navbar', ['smallScreenService', function(smallScreenService) {
        return {
            restrict: 'A',
            templateUrl: 'navbar/navbar.html',
            replace: true,
            link: function(scope) {
                scope.enableSmallScreenMode = function() {
                    smallScreenService.set(true);
                }
            }
        };
    }])
})();