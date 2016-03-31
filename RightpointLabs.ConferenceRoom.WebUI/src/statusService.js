(function() {
    'use strict;'

    angular.module('app').service('statusService', ['timeService', function(timeService) {
        function status(current, meetings) {
            var now = timeService.now();
            if (!current) {
                return { busy: false, freeAt: 0, freeFor: null };
            }
            if(!current.IsStarted && moment(current.Start).isAfter(now))
            {
                if(moment(now).startOf('day').add(1, 'days').isBefore(moment(current.Start))) {
                    // nothing else today
                    return { busy: false, freeAt: 0, freeFor: null };
                }
                var freeFor = -now.diff(current.Start, 'minutes', true);
                return { busy: false, until: moment(current.Start), duration: freeFor, freeAt: 0, freeFor: freeFor };
            }
            var until = current.End;
            meetings.forEach(function (a) {
                if (a.Start == until) {
                    until = a.End;
                }
            });
            var following = meetings.filter(function(m) { m.Start > until });
            following.sort(function(a, b) { return a.Start < b.Start ? -1 : 1; });
            var nextStart = (following[0] || {}).Start;
            
            if(moment(now).startOf('day').add(1, 'days').isBefore(moment(until))) {
                // nothing else today
                return { busy: true, freeAt: null, freeFor: null };
            }
            return { busy: true, until: moment(until), duration: -now.diff(until, 'minutes', true), freeAt: until, freeFor: nextStart ? moment(until).diff(nextStart, 'minutes', true) : null };
        };
        function statusText(current, meetings) {
            var s = status(current, meetings);
            if(s.busy) {
                if(s.until) {
                    return 'Occupied Until ' + s.until.format('h:mm a');
                } else {
                    return 'Occupied';
                }
            } else {
                if(s.until) {
                    return 'Free Until ' + s.until.format('h:mm a');
                } else {
                    return 'Free';
                }
            }
        }
        
        return {
            status: status,
            statusText: statusText
        }
    }])
})();