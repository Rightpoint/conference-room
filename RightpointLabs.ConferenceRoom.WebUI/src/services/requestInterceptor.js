(function() {
    'use strict';

    angular.module('app').factory('requestInterceptor', ['$q', 'logger', 'spinner', function($q, logger, spinner){
        function startRequest(arg) {
            spinner.startRequest();
            return $q.when(arg);
        }
        function endRequest() {
            spinner.endRequest();
        }
        function endReject(arg) {
            endRequest();
            var logIt = (arg.config || { logFailure: true }).logFailure !== false;
            if(logIt) {
                if(arg){
                    logger.error(arg.data || "Unknown error communicating with server", arg.statusText || "Unknown error");
                } else{
                    logger.error("Unknown error communicating with server", "Unknown error");
                }
            }
            return $q.reject(arg);
        }
        function endSuccess(arg) {
            endRequest();
            return $q.when(arg);
        }

        return {
            request: startRequest,
            requestError: endReject,
            response: endSuccess,
            responseError: endReject
        };
    }]);
})();