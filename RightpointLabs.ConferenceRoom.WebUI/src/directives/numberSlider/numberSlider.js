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