(function() {
    'use strict';

    angular.module('app').directive('bigSpinner', function() {
       return {
           template: '<div></div>',
           restrict: 'E',
           scope: {
               active: '='
           },
           link: function(scope, element) {
               var spin = new Spinner({
                   color: '#fff',
                   lines: 13,
                   radius: 30,
                   length: 40,
                   width: 15
               });
               scope.$watch('active', function(val) {
                   if(val) {
                       spin.spin(element[0]);
                   } else {
                       spin.stop();
                   }
               });
           }
       }
    });
})();