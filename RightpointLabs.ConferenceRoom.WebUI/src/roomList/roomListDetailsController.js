(function() {
    'use strict;'

    angular.module('app').controller('RoomListDetailsController', ['Restangular', '$stateParams', function(Restangular, $stateParams) {
        var self = this;
        self.roomLists = [];
        Restangular.one('roomList', $stateParams.roomListAddress).getList('rooms').then(function(data) {
            self.rooms = data;
        });
    }]);
})();