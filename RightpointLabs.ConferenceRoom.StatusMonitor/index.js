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

var LedManager = require('./ledManager.js');
var led = new LedManager(config);

// test colors
led.setColor(0, 0, 0, 0);
led.setColor(1, 0, 0, 400);
setTimeout(function() {
    led.setColor(0, 1, 0, 400);
    setTimeout(function() {
        led.setColor(0, 0, 1, 400);
        setTimeout(function() {
            // and start
            led.setColor(0, 0, 0, 400);
            setTimeout(function() {
                updateIn(1);
                start();
            }, 500);
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
    led.setCycle([ 
       { state: { red: 1, green: 0, blue: 0 }, duration: 200 }, 
       { state: { red: 1, green: 0, blue: 0 }, duration: 2000 }, 
       { state: { red: 0.625, green: 0.125, blue: 0.9375 }, duration: 200 }
    ]);
}

function orange() {
    console.log('orange');
    led.setCycle([ 
       { state: { red: 1, green: 0, blue: 0 }, duration: 200 }, 
       { state: { red: 1, green: 0, blue: 0 }, duration: 5000 }, 
       { state: { red: 1, green: 0.5, blue: 0 }, duration: 200 }
    ]);
}

function red() {
    console.log('red');
    led.setColor(1, 0, 0, 1000);
}
function green() {
    console.log('green');
    led.setColor(0, 1, 0, 1000);
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
    client.serviceHandlers.connectionLost = function() {
        console.log('signalR connection lost');
    };
    client.serviceHandlers.connectFailed = function() {
        console.log('signalR connection failed');
    };
    client.serviceHandlers.reconnecting = function() {
        console.log('signalR reconnecting');
	return true;
    };
    client.serviceHandlers.reconnected = function() {
        console.log('signalR reconnected');
    };
    client.serviceHandlers.onerror = function(error) {
        console.log('signalR error: ' + error);
    };
}
// wait
