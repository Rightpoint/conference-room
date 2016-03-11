(function() {
    'use strict;'

    angular.module('app').service('statusService', [function() {
        function status(current, meetings) {
            var now = moment();
            if (!current) {
                return { busy: false };
            }
            if(!current.IsStarted && moment(current.Start).isAfter(now))
            {
                if(moment(now).startOf('day').add(1, 'days').isBefore(moment(current.Start))) {
                    // nothing else today
                    return { busy: false };
                }
                return { busy: false, until: moment(current.Start), duration: -now.diff(current.Start, 'minutes', true) };
            }
            var until = current.End;
            meetings.forEach(function (a) {
                if (a.Start == until) {
                    until = a.End;
                }
            });
            if(moment(now).startOf('day').add(1, 'days').isBefore(moment(until))) {
                // nothing else today
                return { busy: true };
            }
            return { busy: true, until: moment(until), duration: -now.diff(until, 'minutes', true) };
        };
        function statusText(current, meetings) {
            var s = status(current, meetings);
            if(s.busy) {
                if(s.until) {
                    return 'Busy until ' + s.until.format('h:mm a');
                } else {
                    return 'Busy';
                }
            } else {
                if(s.until) {
                    return 'Free until ' + s.until.format('h:mm a');
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