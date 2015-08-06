(function() {
    'use strict;'

    angular.module('app').controller('SettingsController', ['settings', '$modal', function(settings, $modal) {
        var self = this;
        
        self.isLocked = true;
        self.settings = settings;
        
        self.unlock = function () {
            $modal.open({
                templateUrl: 'settings/modal.html'
            }).result.then(function (data) {
                window.alert(data);
            })
        };
    }]);
})();