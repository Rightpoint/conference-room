(function() {
    'use strict;'

    angular.module('app').factory('activityService', ['UpdateHub', '$rootScope', '$timeout', 'authToken', function(UpdateHub, $rootScope, $timeout, authToken) {
        var blockTimer = null;
        function sendActive() {
            if(blockTimer) {
                return;
            }
            blockTimer = $timeout(function() {
               blockTimer = null;
            }, 10000);
            UpdateHub.clientActive(authToken);
        }
        var resetEvents = 'mousedown mouseover mouseout mousemove touch touchmove touchend';
        angular.element(document).on(resetEvents, sendActive);
        $rootScope.$on('$destroy', function() {
            angular.element(document).off(resetEvents, sendActive);
        });
        return {};
    }])
})();