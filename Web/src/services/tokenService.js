(function() {
    'use strict;'

    angular.module('app').factory('tokenService', ['$http', '$rootScope', '$q', function($http, $rootScope, $q) {
        var obj = { tokenInfo: {} };

        function updateTokenInfo() {
            obj.tokenInfo = $http.get('/api/tokens/info').then(function(data) {
                console.log('got token', data.data);
                tokenInfo = $q.when(data.data);
                $rootScope.$broadcast('tokenInfoChanged', data.data);
                return data.data;
            });
        }
        updateTokenInfo();

        $rootScope.$on('deviceChanged', function(evt, device) {
            obj.tokenInfo.then(function(ti) {
                if(ti.device == device) {
                    updateTokenInfo();
                }
            });
        });

        return obj;
    }])
})();