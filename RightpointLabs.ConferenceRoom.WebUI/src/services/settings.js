(function() {
    'use strict;'

    angular.module('app').factory('settings', ['localStorageService', '$rootScope', function(localStorageService, $rootScope) {
        var settings = localStorageService.get('settings') || {};
        localStorageService.set('settings', settings);
        
        var obj = {};
        obj.onChange = function (key, newValue, oldValue) {
            console.log(key, newValue, oldValue);
            if (key == 'isSmallScreen') {
                if(!!newValue) {
                    $("html").addClass('small-screen');
                } else{
                    $("html").removeClass('small-screen');
                }
            }
            $rootScope.$emit('settingChanged');
            $rootScope.$emit('settingChanged.' + key);
        };
        angular.forEach(['isSmallScreen', 'defaultRoom', 'securityKey'], function (i) {
            Object.defineProperty(obj, i, {
                get: function () { return settings[i]; },
                set: function (value) {
                    var oldValue = settings[i];
                    settings[i] = value;
                    obj.onChange(i, value, oldValue);
                    localStorageService.set('settings', settings);
                }
            });

            // trigger one 'change' to initialize things
            obj.onChange(i, obj[i], obj[i]);
        });
        
        return obj;
    }])
})();