(function() {
    'use strict';

    angular.module('app').service('timelineService', [function(){
        return {
            build: function buildTimeline(start, end, appointments) {
                var totalMinutes = end.diff(start, 'minute', true);

                if(!appointments) {
                    return [{
                        size: 1,
                        isLoading: true
                    }];
                }
                var ranges = [];
                var current = start;
                _.each(appointments, function(item) {
                    var itemStart = moment(item.Start);
                    var itemEnd = moment(item.End);
                    if(!itemStart.isBefore(end) || !itemEnd.isAfter(current)){
                        return;
                    }
                    var freeTime = itemStart.diff(current, 'minute', true);
                    if(freeTime > 0) {
                        ranges.push({
                            size: freeTime/totalMinutes
                        });
                    }
                    var time = moment.min(itemEnd, end).diff(itemStart, 'minute', true);
                    if(freeTime < 0) {
                        // we're double-booked... um... just take it off the second one?
                        time += freeTime;
                    }
                    if(time > 0) {
                        ranges.push({
                            size: time/totalMinutes,
                            appointment: item
                        })
                    }

                    current = itemEnd;
                });

                if(current.isBefore(end)) {
                    ranges.push({
                        size: end.diff(current, 'minute', true)/totalMinutes
                    });
                }

                return ranges;
            }
        };
    }]);
})();