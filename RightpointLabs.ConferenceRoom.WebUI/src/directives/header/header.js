(function() {
    'use strict;'

    angular.module('app').directive('header', ['tokenService', 'activityService', function(tokenService, activityService) {
        return {
            restrict: 'E',
            templateUrl: 'directives/header/header.html',
            replace: true,
            scope: {
                title: '@'
            },
            link: function (scope, element, attr) {
                scope.isDevice = true;
                
                function update(tokenInfo) {
                    console.log(tokenInfo);
                    scope.isDevice = !!tokenInfo.device;
                    scope.defaultRoom = tokenInfo.controlledRooms && tokenInfo.controlledRooms.length && tokenInfo.controlledRooms[0];
                    scope.building = tokenInfo.building;
                }
                tokenService.tokenInfo.then(update);
                scope.$on('tokenInfoChanged', function(evt, args) { update(args); });
            }
        };
    }])
})();