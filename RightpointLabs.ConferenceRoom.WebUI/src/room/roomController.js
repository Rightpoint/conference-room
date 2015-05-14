(function() {
    'use strict;'

    angular.module('app').controller('RoomController', ['Restangular', '$stateParams', function(Restangular, $stateParams) {
        var self = this;
        self.displayName = 'Loading...';
        self.roomAddress = $stateParams.roomAddress;
        var room = Restangular.one('room', self.roomAddress);
        room.one('info').get({ securityKey: '' }).then(function(data) {
            self.displayName = data.DisplayName;
        });
        room.getList('schedule').then(function(data) {
            self.appointments = data;
        });
    }]);
})();