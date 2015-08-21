(function() {
    'use strict';
    angular.module('app').config(['$provide', function($provide) {
        $provide.decorator('$log', ['$delegate', '$injector', function($delegate, $injector) {
            function addHook(method) {
                var old = $delegate[method];
                $delegate[method] = function() {
                    var args = [].slice.call(arguments);
                    var message = args;
                    if(message.length == 1 && _.isString(message[0])) {
                        message = message[0];
                    }
                    $injector.get('remoteLogger').send(method, message);
                    old.apply(null, args);
                };
            }
            addHook('log');
            addHook('debug');
            addHook('info');
            addHook('warn');
            addHook('error');
            
            return $delegate;
        }]);
    }]);
})();