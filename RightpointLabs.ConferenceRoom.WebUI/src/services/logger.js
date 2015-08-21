(function() {
    'use strict';

    angular.module('app').factory('logger', ['$log', 'toastr', function($log, toastr){
        toastr.options.timeOut = 2000; // milliseconds
        toastr.options.positionClass = 'toast-top-full-width';

        function error(message, title, options) {
            toastr.error(message, title, options ||
                {
                    'closeButton': true,
                    'timeOut': 65000, // wait 65s for errors (generally retry @ 60s)
                    'extendedTimeOut': 15000 // 15s after they hover it
                });
            $log.error(message);
        }

        function info(message, title, options) {
            toastr.info(message, title, options);
            $log.info(message);
        }

        function success(message, title, options) {
            toastr.success(message, title, options);
            $log.debug('Success', message);
        }

        function warning(message, title, options) {
            toastr.warning(message, title, options);
            $log.warn(message);
        }

        function log(message) {
            $log.log(message);
        }

        return {
            error: error,
            info: info,
            success: success,
            warning: warning,
            log: log
        };
    }]);
})();