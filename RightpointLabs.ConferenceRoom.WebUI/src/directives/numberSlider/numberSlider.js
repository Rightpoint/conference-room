(function() {
    'use strict;'

    angular.module('app').directive('numberSlider', [function() {
        return {
            restrict: 'E',
            templateUrl: 'directives/numberSlider/numberSlider.html',
            replace: true,
            scope: {
                selectedNumber: '=',
                allowedNumbers: '='
            },
            link: function (scope, element, attr) {
                scope.selectedNumber = scope.selectedNumber || 30;
                scope.render = function(n) {
                    var now = moment();
                    var minute = now.minute() % 15;
                    now.minute(minute).second(0).millisecond(0);
                    now.add(n, 'minutes');
                    return n.format('h:mm');
                };
                function update() {
                    if(!_.some(scope.allowedNumbers, function(i) { return i == scope.selectedNumber; })) {
                        scope.selectedNumber = _.last(scope.allowedNumbers) || 0;
                    }
                    scope.prevNumber = _.last(scope.allowedNumbers.filter(function(i) { return i < scope.selectedNumber; }));
                    scope.nextNumber = _.first(scope.allowedNumbers.filter(function(i) { return scope.selectedNumber < i; }));
                }
                
                scope.$watchCollection('[ selectedNumber, allowedNumbers ]', update);
                scope.next = function() {
                    if(scope.nextNumber) {
                        scope.selectedNumber = scope.nextNumber;
                    }
                };
                scope.prev = function() {
                    if(scope.prevNumber) {
                        scope.selectedNumber = scope.prevNumber;
                    }
                };
            }
        };
    }])
})();