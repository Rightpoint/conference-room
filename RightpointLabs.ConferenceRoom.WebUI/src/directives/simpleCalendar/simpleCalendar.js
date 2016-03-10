(function() {
    'use strict;'

    angular.module('app').directive('simpleCalendar', ['$timeout', '$interval', '$window', function($timeout, $interval, $window) {
        return {
            restrict: 'E',
            templateUrl: 'directives/simpleCalendar/simpleCalendar.html',
            replace: true,
            scope: {
                calendars: '=',
                showTitles: '=',
                click: '&',
            },
            link: function (scope, element, attr) {
                var pxPerHour = parseInt(attr.pxPerHour || 75);
                var minHeight = parseInt(attr.minHeight || 24);
                var vMargin = parseInt(attr.vMargin || 2);
                var hours = parseInt(attr.hours || 72);
                scope.vHeight = pxPerHour * hours;
                
                scope.styles = function styles(evt) {
                    var top = scope.getOffset(evt.Start);
                    var bottom = scope.getOffset(evt.End);
                    return {
                        top: top,
                        height: Math.max(bottom - top - vMargin, minHeight)
                    };
                };
                scope.formatHour = function formatHour(value) {
                    return moment(value).format('h a');
                };
                scope.formatTime = function formatTime(value) {
                    return moment(value).format('h:mm a');
                };
                scope.status = function status(calendar) {
                    var current = calendar.CurrentMeeting;
                    if (!current) {
                        return 'Free';
                    }
                    if(!current.IsStarted && moment(current.Start).isAfter(scope.now))
                    {
                        if(moment(scope.now).startOf('day').add(1, 'days').isBefore(moment(current.Start))) {
                            // nothing else today
                            return 'Free';
                        }
                        return 'Free until ' + scope.formatTime(current.Start);
                    }
                    var until = current.End;
                    calendar.NearTermMeetings.forEach(function (a) {
                        if (a.Start == until) {
                            until = a.End;
                        }
                    });
                    if(moment(scope.now).startOf('day').add(1, 'days').isBefore(moment(until))) {
                        // nothing else today
                        return 'Busy';
                    }
                    return 'Busy until ' + scope.formatTime(until);
                };
                scope.freeBusy = function freeBusy(calendar) {
                    var current = calendar.CurrentMeeting;
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
                        return Math.min(Math.max(moment(value).diff(today, 'minutes') / 60 * pxPerHour, 0), pxPerHour * hours);
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
                    angular.element(scroll).on('scroll', function() {
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