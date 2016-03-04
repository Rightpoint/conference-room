(function() {
    'use strict;'

    angular.module('app').controller('SettingsController', ['settings', '$modal', 'Restangular', '$interval', function(settings, $modal, Restangular, $interval) {
        var self = this;
        
        self.isLocked = false;
        self.settings = settings;
        self.isAuthorized = false;
        
        function updateStatus() {
            if (!settings.defaultRoom) {
                return;
            }
            return Restangular.one('room', settings.defaultRoom).one('info').get({ securityKey: settings.securityKey }).then(function (data) {
                self.isAuthorized = data.SecurityStatus == 3
            });
        }
        $interval(updateStatus, 60000);
        updateStatus();
        
        self.reload = function () {
            window.location.reload();
        };
        
        self.unlock = function () {
            $modal.open({
                templateUrl: 'settings/unlock.html'
            }).result.then(function (data) {
                self.isUnlocking = true;
                Restangular.one('settings').post('checkCode', {}, { code: data }).then(function (result) {
                    self.isUnlocking = false;
                    if (result) {
                        self.isLocked = false;
                    }
                }, function () {
                    self.isUnlocking = false;
                });
            })
        };

        self.pickRoom = function () {
            $modal.open({
                templateUrl: 'settings/pickRoom.html',
                controller: 'PickRoomController',
                controllerAs: 'c'
            }).result.then(function (roomAddress) {
                if (roomAddress) {
                    if(!settings.securityKey) {
                        settings.securityKey = (''+Math.random()).replace('.','');
                    }
                    return Restangular.one('room', roomAddress).post('requestAccess', {}, { securityKey: settings.securityKey }).then(function() {
                        settings.defaultRoom = roomAddress;
                        updateStatus();
                    });
                }
            })
        };
    }]);
})();