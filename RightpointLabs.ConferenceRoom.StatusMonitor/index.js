var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var gpio = require('rpi-gpio');
var async = require('async');
var signalR = require('signalr-client');

var config = JSON.parse(fs.readFileSync('config.json'));

async.parallel([
    function(c) {
        gpio.setup(config.redPin, gpio.DIR_OUT, c);
    },
    function(c) {
        gpio.setup(config.greenPin, gpio.DIR_OUT, c);
    }
], function (e, results) {
    console.log('gpio setup');
    console.log(e);
    console.log(results);
    setPins(false, false);
    updateIn(1);
});

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
    setPins(true, false);
}
function green() {
    console.log('green');
    setPins(false, true);
}
function setPins(red, green) {
    async.parallel([
        function(c) {
            gpio.write(config.redPin, red, c);
        },
        function(c) {
            gpio.write(config.greenPin, green, c);
        }
    ], function (e, results) {
        console.log('set pins');
        console.log(e);
        console.log(results);
    });
}

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

// wait
