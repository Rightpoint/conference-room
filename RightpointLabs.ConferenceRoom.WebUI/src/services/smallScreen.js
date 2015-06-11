(function() {
    'use strict;'

    angular.module('app').service('smallScreenService', ['localStorageService', '$rootScope', function(localStorageService, $rootScope) {
        var isSmallScreen = !!localStorageService.get('isSmallScreen');
        update();
        function update() {
            if(isSmallScreen) {
                $("html").addClass('small-screen');
            } else{
                $("html").removeClass('small-screen');
            }
        }
        return {
            get: function() {
                return isSmallScreen;
            },
            set: function(newValue) {
                isSmallScreen = !!newValue;
                localStorageService.set('isSmallScreen', isSmallScreen);
                update();
                $rootScope.$emit('smallScreenChanged');
            }
        }
    }])
})();