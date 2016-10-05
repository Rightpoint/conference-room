var fs = require('fs');

module.exports = function Backlight(config) {
    function isPeak() {
        var hour = new Date().getHours();
        return config.peakStart <= hour && hour < config.peakEnd;
    }
    function on() {
        var value = isPeak() ? config.on : config.onOffPeak || config.on;
        fs.writeFileSync(config.controlFile, value, "utf8");
    }
    function off() {
        var value = isPeak() ? config.off : config.offOffPeak || config.off;

        fs.writeFileSync(config.controlFile, value, "utf8");
    }

    function poll() {
        timeout = setTimeout(function() {
            poll();
            off();
        }, 60000); 
    }

    var timeout = null;
    function keepAlive() {
        if(timeout) {
            clearTimeout(timeout);
        }
        on();
        timeout = setTimeout(function() {
            poll();
            off();
        }, config.delay || 5000); 
    }

    keepAlive();

    return {
        keepAlive: keepAlive
    }
};