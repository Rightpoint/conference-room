(function() {
    'use strict;'

    angular.module('app').service('timeService', ['$rootScope', '$timeout', function($rootScope, $timeout) {
        var delta = null;
        function setCurrentTime(time) {
            delta = moment().diff(time);
        }
        var timeout = null;
        function scheduleNext() {
            if(timeout) {
                $timeout.cancel(timeout);
            }
            
            var t = now().startOf('minute').add(1, 'minute').diff(now());
            timeout = setTimeout(function() {
                timeout = null;
                $rootScope.$broadcast('timeChanged');
                scheduleNext();
            }, t);
        }
        
        function now() {
            return moment().add(delta || 0, 'ms');
        }

        return {
            setCurrentTime: setCurrentTime,
            now: now
        };
    }])
})();