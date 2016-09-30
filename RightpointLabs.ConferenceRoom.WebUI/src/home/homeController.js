(function() {
    'use strict;'

    angular.module('app').controller('HomeController', ['tokenService', '$state', '$scope', function(tokenService, $state, $scope) {
        var self = this;
        self.isLoading = true;
        self.device = null;

        function checkToken(tokenInfo) {
            self.isLoading = false;

            console.log('checkToken', tokenInfo);

            if(tokenInfo.controlledRooms && tokenInfo.controlledRooms.length) {
                $state.go('room', { roomAddress: tokenInfo.controlledRooms[0] });
                return;
            }

            if(tokenInfo.device) {
                self.device = tokenInfo.device;
            }
            else {
                $state.go('roomList');
            }
        }

        $scope.$on('tokenInfoChanged', function(evt, args) { checkToken(args); });
        tokenService.tokenInfo.then(checkToken);
    }]);
})();