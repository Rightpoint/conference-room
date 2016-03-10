(function() {
    'use strict;'

    angular.module('app').directive('simpleCalendar', ['$timeout', '$interval', '$window', function($timeout, $interval, $window) {
        return {
            restrict: 'E',
            templateUrl: 'directives/simpleCalendar/simpleCalendar.html',
            replace: true,
            scope: {
                calendars: '=',
                showTitles: '='
            },
            link: function (scope, element, attr) {
                var pxPerHour = 200;
                var minHeight = 50;
                var hours = parseInt(attr.hours || 72);
                scope.vHeight = pxPerHour * hours;
                
                scope.styles = function styles(evt) {
                    var top = scope.getOffset(evt.Start);
                    var bottom = scope.getOffset(evt.End);
                    return {
                        top: top,
                        height: Math.max(bottom - top, minHeight)
                    };
                };
                scope.formatHour = function formatHour(value) {
                    return moment(value).format('h a');
                };
                scope.formatTime = function formatTime(value) {
                    return moment(value).format('h:mm a');
                };
                scope.status = function status(calendar) {
                    var current = calendar.Current;
                    if (!current) {
                        return 'Free';
                    }
                    if(!current.IsStarted && moment(current.Start).isAfter(scope.now))
                    {
                        return 'Free until ' + scope.formatTime(current.Start);
                    }
                    var until = current.End;
                    calendar.Events.forEach(function (a) {
                        if (a.Start == until) {
                            until = a.End;
                        }
                    });
                    return 'Busy until ' + scope.formatTime(until);
                };
                scope.freeBusy = function freeBusy(calendar) {
                    var current = calendar.Current;
                    if (!current) {
                        return 'free';
                    }
                    if(!current.IsStarted && moment(current.Start).isAfter(scope.now))
                    {
                        return 'free';
                    }
                    return 'busy';
                };

                function update() {
                    var now = moment();
                    scope.now = now.toDate();
                    var today = moment(now).startOf('day');
                    scope.getOffset = function getOffset(value) {
                        return moment(value).diff(today, 'minutes') / 60 * 50;
                    };
                    scope.hours = _.range(0, hours).map(function (i) { return moment(today).add(i, 'hours').toDate(); });

                    var area = element.find('.v-scrollable');
                    area[0].scrollTop = scope.getOffset(now) - minHeight;
                }
                
                scope.$watch('calendars', update, true);
                $interval(update, 60000);
                
                var scrolls = element.find('.h-scrollable');
                for(var i=0; i<scrolls.length; i++) {
                    var scroll = scrolls[i];
                    console.log('hooking', scroll);
                    angular.element(scroll).on('scroll', function() {
                        console.log(this);
                        var l = this.scrollLeft;
                        for(var i=0; i<scrolls.length; i++) {
                            scrolls[i].scrollLeft = l;
                        }
                    });
                }
            }
        };
    }])
})();