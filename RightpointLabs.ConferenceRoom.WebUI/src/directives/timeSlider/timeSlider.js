(function() {
    'use strict;'

    angular.module('app').directive('timeSlider', ['timeService', function(timeService) {
        return {
            restrict: 'E',
            templateUrl: 'directives/timeSlider/timeSlider.html',
            replace: true,
            scope: {
                selectedMinutes: '=',
                selectedTime: '=',
                maxTime: '='
            },
            link: function (scope, element, attr) {
                var meetTimes = [15, 30, 45, 60, 90, 120];
                scope.selectedMinutes = scope.selectedMinutes || 30;
                function update() {
                    var now = timeService.now();
                    var minute = now.minute();
                    minute -= minute % 15;
                    now.minute(minute).second(0).millisecond(0);
                    
                    var allowed = [].concat(meetTimes);
                    if(scope.maxTime) {
                        // make sure the max value is a valid value
                        var maxM = moment(scope.maxTime).diff(now, 'minutes');
                        if(maxM > 0 && maxM < 120) {
                            allowed = _.uniq(meetTimes.concat([maxM]));
                            allowed.sort(function(a,b) { return a < b ? -1 : 1; });
                        }
                    }

                    allowed = allowed.map(function(i) {
                        var time = moment(now).add(i, 'minutes');
                        return {
                            minutes: i,
                            time: time,
                            formattedTime: time.format('h:mm')
                        }
                    }).filter(function(i) {
                        return !scope.maxTime || i.time.isSameOrBefore(scope.maxTime);
                    });
                    if(scope.selectedMinutes > _.last(allowed).minutes) {
                        scope.selectedMinutes = _.last(allowed).minutes;
                    }
                    scope.selectedObj = _.findWhere(allowed, { minutes: scope.selectedMinutes });
                    scope.selectedMinutes = scope.selectedObj.minutes;
                    scope.selectedTime = scope.selectedObj.time.format();
                    scope.prevObj = _.last(allowed.filter(function(i) { return i.minutes < scope.selectedMinutes; }));
                    scope.nextObj = _.first(allowed.filter(function(i) { return scope.selectedMinutes < i.minutes; }));
                }
                
                scope.$watchCollection('[ selectedMinutes, maxTime ]', update);
                scope.$on('timeChanged', update);

                scope.next = function() {
                    if(scope.nextObj) {
                        scope.selectedMinutes = scope.nextObj.minutes;
                    }
                };
                scope.prev = function() {
                    if(scope.prevObj) {
                        scope.selectedMinutes = scope.prevObj.minutes;
                    }
                };
            }
        };
    }])
})();