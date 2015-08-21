(function() {
    'use strict;'

    angular.module('app').factory('soundService', ['$log', function ($log) {
        function play(file) {
            try {
                if ("Audio" in window) {
                    var a = new Audio();
                    a.src = file;
                    a.autoplay = true;
                    return;
                }
            } catch (e){
                $log.warn('Cannot play', file, e);
            }
        }
        
        return {
            play: play
        };
    }])
})();