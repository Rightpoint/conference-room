var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var signalR = require('signalr-client');
var path = require('path');
var pwm = require('pi-blaster.js');

var configFile = path.join(__dirname, 'config.json');
console.log('Loading configuration from ' + configFile);
var config = JSON.parse(fs.readFileSync(configFile));

// test colors
setPins(false, false, false);
setPins(true, false, false);
setTimeout(function() {
    setPins(false, true, false);
    setTimeout(function() {
        setPins(false, false, true);
        setTimeout(function() {
            // and start
            setPins(false, false, false);
            updateIn(1);
            start();
        }, 500);
    }, 500);
}, 500);

function getStatus() {
    return new Promise(function(resolve, reject) {
        var options = url.parse(config.apiServer + "/room/" + config.room + "/status");
        options.method = "GET";
        return http.request(options, function(e) {
            data = "";
            e.on('data', function(c) { data += String(c); });
            e.on('end', function() { resolve(data); });
        }).end();
    })
}

var updateTimeout = null;
function updateIn(delay) {
    if(null != updateTimeout){
        clearTimeout(updateTimeout);
    }
    updateTimeout = setTimeout(function() {
        getStatus().then(function(data) {
            var obj = JSON.parse(data);
            var status = obj.Status;
            switch(status) {
                case 0:
                    green();
                    break;
                case 1:
                    red();
                    break;
                case 2:
                    red();
                    break;
                default:
                    console.log('invalid status: ' + status);
                    break;
            }
            if(obj.NextChangeSeconds) {
                updateIn((obj.NextChangeSeconds + 1) * 1000); // server advises us to check back at this time
            }
        });
    }, delay);
}

function red() {
    console.log('red');
    setPins(true, false, false);
}
function green() {
    console.log('green');
    setPins(false, true, false);
}
function setPins(red, green, blue) {
    pwm.setPwm(config.red.pin, 0);
    pwm.setPwm(config.green.pin, 0);
    pwm.setPwm(config.blue.pin, 0);
    pwm.setPwm(config.red.pin, red ? config.red.brightness : 0);
    pwm.setPwm(config.green.pin, green ? config.green.brightness : 0);
    pwm.setPwm(config.blue.pin, blue ? config.blue.brightness : 0);
    console.log('set pins');
}

function start() {
    setInterval(function() {
        updateIn(5000);
    }, 5 * 60 * 1000);

    var client  = new signalR.client(
        config.signalRServer,
        ['UpdateHub']
    );
    client.on('UpdateHub', 'Update', function(room) {
        console.log('got notification of change to ' + room);
        if(config.room == room) {
            updateIn(1);
        }
    });
}
// wait
