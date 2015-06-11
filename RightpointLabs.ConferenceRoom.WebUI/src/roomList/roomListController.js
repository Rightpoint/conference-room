(function() {
    'use strict;'

    angular.module('app').controller('RoomListController', ['Restangular', 'localStorageService', '$scope', '$state', '$timeout', function(Restangular, localStorageService, $scope, $state, $timeout) {
        var self = this;
        self.roomLists = [];
        Restangular.all('roomList').getList().then(function(data) {
            self.roomLists = data;
        });

        var defaultRoom = localStorageService.get('defaultRoom');
        self.hasDefaultRoom = !!defaultRoom;
        if(self.hasDefaultRoom) {
            // we have a default room we're supposed to be managing - time out and go there after 60 seconds
            var timeout = $timeout(function() {
                $state.go('home');
            }, 60000);
            $scope.$on('$destroy', function() {
                $timeout.cancel(timeout);
            });
        }
    }]);
})();