(function() {
    'use strict;'

    angular.module('app').directive('timeSlider', [function() {
        return {
            restrict: 'E',
            templateUrl: 'directives/timeSlider/timeSlider.html',
            replace: true,
            scope: {
                selectedTime: '=',
                maxTime: '='
            },
            link: function (scope, element, attr) {
                var meetTimes = [15, 30, 45, 60, 90, 120];
                scope.selectedTime = scope.selectedTime || 30;
                function update() {
                    var allowed = meetTimes;
                    meetTimes = meetTimes.filter(function(i) {
                        return i <= scope.maxTime;
                    });
                    if(scope.selectedTime > scope.maxTime) {
                        scope.selectedTime = _.last(meetTimes);
                    }
                    scope.prevTime = _.last(meetTimes.filter(function(i) { return i < scope.selectedTime; }));
                    scope.nextTime = _.first(meetTimes.filter(function(i) { return scope.selectedTime < i; }));
                }
                
                scope.$watchCollection('[ selectedTime, maxTime ]', update);
                scope.next = function() {
                    if(scope.nextTime) {
                        scope.selectedTime = scope.nextTime;
                    }
                };
                scope.prev = function() {
                    if(scope.prevTime) {
                        scope.selectedTime = scope.prevTime;
                    }
                };
            }
        };
    }])
})();