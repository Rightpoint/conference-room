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
setPins(0, 0, 0);
setPins(1, 0, 0);
setTimeout(function() {
    setPins(0, 1, 0);
    setTimeout(function() {
        setPins(0, 0, 1);
        setTimeout(function() {
            // and start
            setPins(0, 0, 0);
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

var applyInterval = null;
var updateTimeout = null;
function updateIn(delay) {
    if(null != updateTimeout){
        clearTimeout(updateTimeout);
    }
    updateTimeout = setTimeout(function() {
        getStatus().then(function(data) {
            var obj = JSON.parse(data);
            var status = obj.Status;
            if(null != applyInterval) {
                clearInterval(applyInterval);
                applyInterval = null;
            }
            var lastApply = new Date().getTime();
            function apply() {
                var thisApply = new Date().getTime();
                obj.RoomNextFreeInSeconds -= (thisApply - lastApply) / 1000;
                lastApply = thisApply;
                switch(status) {
                    case 0:
                        green();
                        break;
                    case 1:
                        if(obj.RoomNextFreeInSeconds < 600) {
                            orange();
                        } else {
                            red();
                        }
                        break;
                    case 2:
                        if(obj.CurrentMeeting && obj.CurrentMeeting.IsNotManaged) {
                            // non-managed meetings don't need to be started
                            red();
                        } else {
                            // this is a managed meeting that's on the verge of getting auto-cancelled - look wierd.
                            purple();
                        }
                        break;
                    default:
                        console.log('invalid status: ' + status);
                        break;
                }
            }
            apply();
            if(status == 1) {
                applyInterval = setInterval(apply, 10000);
            }

            if(obj.NextChangeSeconds) {
                updateIn((obj.NextChangeSeconds + 1) * 1000); // server advises us to check back at this time
            }
        });
    }, delay);
}

function purple() {
    console.log('purple');
    setPins(0.625, 0.125, 0.9375);
}

function orange() {
    console.log('orange');
    setPins(1, 0.5, 0);
}

function red() {
    console.log('red');
    setPins(1, 0, 0);
}
function green() {
    console.log('green');
    setPins(0, 1, 0);
}

var lastRed = null;
var lastGreen = null;
var lastBlue = null;
function setPins(red, green, blue) {
    var toSet = [
        { pin: config.red.pin, last: lastRed, now: red },
        { pin: config.green.pin, last: lastGreen, now: green },
        { pin: config.blue.pin, last: lastBlue, now: blue },
    ];

    // make sure we set the ones with the largest decrease in power first, largest increase in power last (to avoid over-driving our power supply due to the transition)
    toSet.forEach(function(i) { i.delta = i.now - (i.last || 0); });
    toSet.sort(function(a,b) { return a.delta < b.delta ? -1 : a.delta > b.delta ? 1 : 0; });
    toSet.forEach(function(i) { pwm.setPwm(i.pin, i.now); });

    lastRed = red;
    lastGreen = green;
    lastBlue = blue;
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
    client.serviceHandlers.connected = function() {
        console.log('signalR connected');
    };
    client.serviceHandlers.onerror = function(error) {
        console.log('signalR error: ' + error);
    }
}
// wait
