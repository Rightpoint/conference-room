(function() {
    'use strict';

    angular.module('app').factory('authInterceptor', ['authToken', function(authToken){
        return {
            request: function(config) {
                config.headers['Authorization'] = "Bearer " + authToken;
                return config;
            }
        };
    }]);
})();