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
                var meetTimes = [15, 30, 45, 60, 75, 90, 105, 120];
                scope.selectedMinutes = scope.selectedMinutes || 30;
                function update() {
                    var now = timeService.now();
                    var realNow = moment(now);
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
                        return (!scope.maxTime || i.time.isSameOrBefore(scope.maxTime)) && i.time.diff(realNow, 'minutes') >= 5;
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
                
                var lastPanDelta = 0;
                var minPanDelta = 25; // adjust this number to adjust the sensitivity of the panning - ie. this is the number of pixels you must pan to move numbers
                scope.panReset = function() {
                    lastPanDelta = 0;
                };
                scope.pan = function(evt) {
                    if(Math.abs(evt.deltaX - lastPanDelta) < minPanDelta) {
                        return;
                    }
                    var obj = evt.deltaX < lastPanDelta ? scope.nextObj : scope.prevObj;
                    lastPanDelta = evt.deltaX;
                    if(obj) {
                        scope.selectedMinutes = obj.minutes;
                    }
                };
            }
        };
    }])
})();