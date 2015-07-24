(function() {
    'use strict;'

    angular.module('app').factory('soundService', [function () {
        function play(file) {
            try {
                if ("Audio" in window) {
                    var a = new Audio();
                    a.src = file;
                    a.autoplay = true;
                    return;
                }
            } catch (e){
                console.log('Cannot play', file, e);
            }
        }
        
        return {
            play: play
        };
    }])
})();