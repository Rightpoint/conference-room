(function() {
    'use strict;'

    angular.module('app').directive('batteryMeter', [function() {
        return {
            restrict: 'E',
            templateUrl: 'directives/batteryMeter/batteryMeter.html',
            replace: true,
            link: function(scope, element, attr) {
                scope.loaded = false;
                if(navigator.getBattery) {
                    navigator.getBattery().then(function(b) {
                        function applyUpdate() {
                            scope.$apply(function() {
                                scope.loaded = true;
                                scope.charging = b.charging;
                                scope.level = b.level * 100;
                            });
                        }
                        b.addEventListener('onchargingchange', applyUpdate);
                        b.addEventListener('onchargingtimechange', applyUpdate);
                        b.addEventListener('ondischargingtimechange', applyUpdate);
                        b.addEventListener('levelchange', applyUpdate);
                        applyUpdate();
                    });
                }
            }
        };
    }])
})();