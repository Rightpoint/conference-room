(function() {
    'use strict;'

    angular.module('app').directive('calendar', ['$timeout', '$interval', function($timeout, $interval) {
        return {
            restrict: 'E',
            templateUrl: 'directives/calendar/calendar.html',
            replace: true,
            scope: {
                data: '='
            },
            link: function (scope, element, attr) {
                var yScale, rawYScale;
                var interval;
                
                function update(data) {
                    var h = element[0].clientHeight;
                    var w = element[0].clientWidth;
                    d3.select(element.find('svg')[0])
                        .attr('height', h).attr('width', w);
                    
                    var e = d3.select(element.find('svg').find('g')[0]);
                    var now = moment();
                    var today = moment(now).startOf('day');
                    var startOfHour = moment(now).add(-15, 'minutes').startOf('hour');
                    
                    var margin = {
                        left: 40,
                        leftBlock: 60,
                        right: 20,
                        rightBlock: 40,
                        top: 10,
                        bottom: 10
                    };
                    
                    rawYScale = d3.scale.linear().domain([startOfHour.toDate(), moment(startOfHour).add(5, 'hours').toDate()]).range([margin.top, h - margin.bottom]);
                    var minH = rawYScale(today.toDate());
                    var maxH = rawYScale(moment(today).add(3, 'days').toDate());
                    if(!yScale) {
                        yScale = rawYScale.copy();
                    }
                    
                    function render() {
                        now = moment();
                        today = moment(now).startOf('day');
                        startOfHour = moment(now).add(-15, 'minutes').startOf('hour');
                        rawYScale = d3.scale.linear().domain([startOfHour.toDate(), moment(startOfHour).add(5, 'hours').toDate()]).range([margin.top, h - margin.bottom]);
                        minH = rawYScale(today.toDate());
                        maxH = rawYScale(moment(today).add(3, 'days').toDate());
                        if(!yScale) {
                            yScale = rawYScale.copy();
                        }

                        var bg = e.selectAll('rect.background').data([0])
                            .attr('transform', 'translate(0,' + minH +')').attr('height', maxH-minH).attr('width', w)
                            .enter().append('rect').attr('class', 'background');

                        var hours = _.range(0, 72).map(function (i) { return moment(today).add(i, 'hours').toDate(); });
                        var hourArea = e.selectAll('g.hour-area').data([0]);
                        hourArea.enter().append('g').attr('class', 'hour-area');

                        var hourLabel = hourArea.selectAll('text.hour').data(hours);
                        hourLabel.enter().append('text').attr('class', 'hour').attr('dy', yScale);
                        hourLabel.text(function(d) { return moment(d).format('h a'); });
                        hourLabel.transition().attr('dy', yScale);

                        var hourLine = hourArea.selectAll('path.hour').data(hours);
                        function hourLineFunc(d) { return d3.svg.line()([ [ margin.left, yScale(d) ], [ w - margin.right, yScale(d) ] ]); }
                        hourLine.enter().append('path').attr('class', 'hour').attr('d', hourLineFunc);
                        hourLine.transition().attr('d', hourLineFunc);
                        
                        var currentLine = e.selectAll('path.current').data([0]);
                        currentLine.enter().append('path').attr('class', 'current').attr('d', function() { return hourLineFunc(now.toDate()); });
                        currentLine.transition().attr('d', function() { return hourLineFunc(now.toDate()); });
                        
                        var meetingsArea = e.selectAll('g.meetings-area').data([0]);
                        meetingsArea.enter().append('g').attr('class', 'meetings-area');

                        var meetings = e.selectAll('g.meeting').data(data || []);
                        function meetingTransform(d) { return 'translate(' + margin.leftBlock + ', ' + yScale(d.Start) + ')'; }
                        function meetingHeight(d) { return Math.max(yScale(d.End) - yScale(d.Start), 20); }
                        var meetingsEnter = meetings.enter().append('g').attr('class', 'meeting').attr('transform', meetingTransform);
                        meetingsEnter.append('rect').attr('width', w - margin.rightBlock - margin.leftBlock).attr('height', meetingHeight);
                        meetingsEnter.append('text').attr({ dx: 5, dy: 5 });
                        meetings.classed({
                            'meeting-past': function(d) { return now.isAfter(d.End); },
                            'meeting-now': function(d) { return now.isSameOrAfter(d.Start) && now.isSameOrBefore(d.End); },
                            'meeting-future': function(d) { return now.isBefore(d.Start); }
                        });
                        meetings.transition().attr('transform', meetingTransform);
                        meetings.selectAll('rect').transition().attr('height', meetingHeight);
                        meetings.selectAll('text').text(function(d) { return d.Organizer; }).transition().attr('dy', function(d) {
                            var mh = yScale(d.Start);
                            if(mh < 0 && mh + meetingHeight(d) > 0) {
                                return Math.min(5 - mh, meetingHeight(d) - 17);
                            }
                            return 5;
                        });;
                    }
                    
                    render();
                    var lastScale = 0;
                    var resetTimeout = null;
                    var zoom = d3.behavior.zoom().scaleExtent([0.2,1000]).scale(1).on('zoom', function() {
                        if(resetTimeout) {
                            $timeout.cancel(resetTimeout);
                            resetTimeout = null;
                        }
                        var evt = d3.event,
                        // now, constrain the x and y components of the translation by the
                        // dimensions of the viewport
                        tx = 0,
                        ty = Math.min(-minH * evt.scale, Math.max(evt.translate[1], (h-maxH) * evt.scale));
                        //console.log(evt.translate[1], h, minH, maxH, evt.scale, maxH - h * evt.scale, ty);
                        // then, update the zoom behavior's internal translation, so that
                        // it knows how to properly manipulate it on the next movement
                        zoom.translate([tx, ty]);
                        
                        if(lastScale !== evt.scale) {
                            yScale.domain(rawYScale.range().map(function(y) { return (y-rawYScale.range()[0]) / evt.scale + rawYScale.range()[0]; }).map(rawYScale.invert));
                            render();
                            e.transition().attr('transform', 'translate(0, ' + ty + ')');
                        } else{
                            e.attr('transform', 'translate(0, ' + ty + ')');
                        }
                        
                        lastScale = evt.scale;
                        
                        resetTimeout = $timeout(function() {
                            console.log('resetting');
                            zoom.scale(1).translate([0,0]).event(e);
                            resetTimeout = null;
                            lastY = 0;
                            lastScale = null;
                            if(resetTimeout) {
                                $timeout.cancel(resetTimeout);
                                resetTimeout = null;
                            }
                        }, 10000);
                    });
                    e.call(zoom);

                    // make sure we run a digest cycle at least once a minute so we can update the 'current' bar and status
                    if(interval) {
                        $interval.cancel(interval);
                        interval = $interval(update, 60000);
                    }
                }
                
                scope.$watch('data', function(data) {
                    scope.realData = (data || []).map(function(i) {
                        return {
                            Organizer: i.Organizer,
                            Start: moment(i.Start).toDate(),
                            End: moment(i.End).toDate()
                        };
                    })
                    update(scope.realData);
                }, true);
            }
        };
    }])
})();