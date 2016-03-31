(function() {
    'use strict;'

    angular.module('app').service('timeService', ['$rootScope', '$timeout', function($rootScope, $timeout) {
        var delta = null;
        function setCurrentTime(time) {
            delta = moment().diff(time);
            scheduleNext();
        }
        var timeout = null;
        function scheduleNext() {
            if(timeout) {
                $timeout.cancel(timeout);
            }
            
            var t = now().startOf('minute').add(1, 'minute').diff(now()) + 100; // extra 100ms just in case the browser fires our timer a bit early
            timeout = $timeout(function() {
                timeout = null;
                $rootScope.$broadcast('timeChanged');
                scheduleNext();
            }, t);
        }
        
        function now() {
            return moment().add(delta || 0, 'ms');
        }
        
        scheduleNext();

        return {
            setCurrentTime: setCurrentTime,
            now: now
        };
    }])
})();