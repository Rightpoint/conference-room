(function() {
    'use strict;'

    angular.module('app').controller('SettingsController', ['settings', function(settings) {
        var self = this;
        
        self.settings = settings;
    }]);
})();