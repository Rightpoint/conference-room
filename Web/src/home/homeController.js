(function() {
    'use strict;'

    angular.module('app').controller('HomeController', ['tokenService', '$state', '$scope', 'UpdateHub', function (tokenService, $state, $scope, UpdateHub) {
        // just asking for UpdateHub so we have the signalR pipeline set up
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
                $state.go('buildingList');
            }
        }

        $scope.$on('tokenInfoChanged', function(evt, args) { checkToken(args); });
        tokenService.tokenInfo.then(checkToken);
    }]);
})();