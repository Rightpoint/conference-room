(function() {
    'use strict;'

    angular.module('app').directive('navbar', ['localStorageService', function(localStorageService) {
        return {
            restrict: 'A',
            templateUrl: 'navbar/navbar.html',
            replace: true,
            link: function(scope) {
                scope.enableKioskMode = function() {
                    localStorageService.set('kioskMode', true);
                    $("html").addClass('kiosk');
                }
                if(localStorageService.get('kioskMode')) {
                    $("html").addClass('kiosk');
                }
            }
        };
    }])
})();