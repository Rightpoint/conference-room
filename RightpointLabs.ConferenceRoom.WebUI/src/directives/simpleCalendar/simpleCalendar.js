(function() {
    'use strict;'

    angular.module('app').directive('simpleCalendar', ['$timeout', '$interval', '$window', 'statusService', function($timeout, $interval, $window, statusService) {
        return {
            restrict: 'E',
            templateUrl: 'directives/simpleCalendar/simpleCalendar.html',
            replace: true,
            scope: {
                calendars: '=',
                showTitles: '=',
                click: '&',
                clickHeader: '&'
            },
            link: function (scope, element, attr) {
                var pxPerHour = parseInt(attr.pxPerHour || 70);
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
                scope.topClass = function($index, calendar) {
                    return angular.merge({ 'first-top': $index === 0 }, calendar.class || {});
                }
                scope.calendarClass = function($index, calendar) {
                    return angular.merge({ 'first-data': $index === 0 }, calendar.class || {});
                }
                scope.eventClass = function(evt) {
                    var isPast = moment(scope.now).isAfter(evt.End) || evt.IsEndedEarly;
                    var isCurrent = (moment(scope.now).isAfter(evt.Start) || evt.IsStarted) && (moment(scope.now).isBefore(evt.End) && !evt.IsEndedEarly);
                    return { 
                        'past-event': isPast,
                        'current-event': isCurrent
                    };
                }
                scope.formatHour = function formatHour(value) {
                    return moment(value).format('h a');
                };
                scope.formatTime = function formatTime(value) {
                    return moment(value).format('h:mm a');
                };
                scope.status = function status(calendar) {
                    return statusService.statusText(calendar.CurrentMeeting, calendar.NearTermMeetings);
                };
                scope.freeBusy = function freeBusy(calendar) {
                    return statusService.status(calendar.CurrentMeeting, calendar.NearTermMeetings).busy ? 'busy' : 'free';
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