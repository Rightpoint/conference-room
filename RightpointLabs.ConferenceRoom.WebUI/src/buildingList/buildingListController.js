(function() {
    'use strict;'

    angular.module('app').controller('BuildingListController', ['Restangular', function(Restangular) {
        var self = this;
        self.isLoading = true;
        self.buildings = [];

        Restangular.all('building').one('all').get({ }).then(function(data) {
            self.isLoading = false;
            self.buildings = data;
        });
    }]);
})();