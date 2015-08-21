(function() {
    'use strict';
    angular.module('app').factory('remoteLogger', ['$http', '$timeout', function($http, $timeout){
        var pending = [];
        var maxMessages = 50;
        var activeCall = null;
        function startCall() {
            if(activeCall)
                return;
            var sending = pending.slice();
            activeCall = $http.post('/api/clientLog/messages', { messages: sending }).then(function() {
                activeCall = null;
                console.log('before', 'sending', sending.length, 'pending', pending.length);
                pending = _.reject(pending, function(i) {
                    return _.some(sending, function(j) { return i.id == j.id; });
                });
                console.log('after', 'sending', sending.length, 'pending', pending.length);
                if(pending.length) {
                    startCall();
                }
            }, function() {
                console.log('failed to send - waiting a bit and trying again');
                activeCall = $timeout(function() {
                    activeCall = null;
                    startCall();
                }, 2000);
            });
        }
        function send(level, message) {
            try {
                if(!_.isString(message)) {
                    message = angular.toJson(message);
                }
            }
            catch(e) {
                // only error could be toJson failing, so I guess we'll just log that
                message = e;
            }
            if(pending.length && pending[pending.length-1].message == message) {
                pending[pending.length-1].count ++;
            } else {
                pending.push({
                    count: 1,
                    id: Math.random(),
                    time: new Date(),
                    level: level,
                    message: message
                });
                while(pending.length > maxMessages) {
                    pending.shift(); // toss old messages
                }
            }
            startCall();
        }

        return {
            send: send,
        };
    }]);
})();