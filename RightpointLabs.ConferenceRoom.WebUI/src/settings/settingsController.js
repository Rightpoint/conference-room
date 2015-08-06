(function() {
    'use strict;'

    angular.module('app').controller('SettingsController', ['settings', '$modal', 'Restangular', function(settings, $modal, Restangular) {
        var self = this;
        
        self.isLocked = true;
        self.settings = settings;
        
        self.unlock = function () {
            $modal.open({
                templateUrl: 'settings/modal.html'
            }).result.then(function (data) {
                self.isUnlocking = true;
                Restangular.one('settings').post('checkCode', {}, { code: data }).then(function (result) {
                    self.isUnlocking = false;
                    console.log(result);
                    if (result) {
                        self.isLocked = false;
                    }
                }, function () {
                    self.isUnlocking = false;
                });
            })
        };
    }]);
})();