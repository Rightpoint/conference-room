(function() {
    'use strict;'

    angular.module('app').controller('RoomListController', ['Restangular', function(Restangular) {
        var self = this;
        self.roomLists = [];
        Restangular.all('roomList').getList().then(function(data) {
            self.roomLists = data;
        });
    }]);
})();