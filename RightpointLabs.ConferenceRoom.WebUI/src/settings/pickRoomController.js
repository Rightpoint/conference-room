(function() {
    'use strict;'

    angular.module('app').controller('PickRoomController', ['settings', '$modal', 'Restangular', '$q', function(settings, $modal, Restangular, $q) {
        var self = this;
        self.lists = [];
        
        self.selected = settings.defaultRoom;

        var p = Restangular.all('roomList').getList().then(function (data) {
            console.log(data);
            return $q.all(data.map(function (l) {
                return Restangular.one('roomList', l.Address).getList('rooms').then(function (data) {
                    console.log(data);
                    l.rooms = data;
                });
            })).then(function () {
                return data;
            });
        }).then(function (data) {
            console.log(data);
            self.lists = data;
        });;
        

    }]);
})();