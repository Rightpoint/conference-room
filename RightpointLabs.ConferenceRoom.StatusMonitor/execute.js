var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var signalR = require('signalr-client');
var path = require('path');
var jwt_decode = require('jwt-decode');

module.exports = function execute(config, led) {
    var deviceToken = jwt_decode(config.deviceKey);
    if(config.bluetooth) {
        var beacon = require('eddystone-beacon');
        var btOptions = {
            name: 'Beacon',
            txPowerLevel: -22,
            tlmCount: 2,
            tlmPeriod: 10
        };
        console.log('Starting bluetooth advertisement');
        beacon.advertiseUid(config.bluetooth.namespace, deviceToken.deviceid, btOptions);
    }

    function getStatus() {
        return new Promise(function(resolve, reject) {
            var options = url.parse(config.apiServer + "/room/" + config.room + "/status");
            options.method = "GET";
            options.headers = { Authorization: "Bearer " + config.deviceKey };
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
                            quickCycle();
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

    function quickCycle() {
        console.log('quickCycle');
        led.setCycle([ 
            { state: { red: 1, green: 0, blue: 0 }, duration: 500 }, 
            { state: { red: 0, green: 1, blue: 0 }, duration: 500 }, 
            { state: { red: 0, green: 0, blue: 1 }, duration: 500 }
        ]);
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
        var gotConnected = false;
        setTimeout(function() {
            if(!gotConnected) {
                console.log('Aborting - no connection');
                process.exit(-1);
            }
        }, 30000);

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
            gotConnected = true;
            console.log('signalR connected');
        };
        client.serviceHandlers.connectionLost = function() {
            console.log('signalR connection lost - aborting');
            process.exit();
        };
        client.serviceHandlers.connectFailed = function() {
            console.log('signalR connection failed - aborting');
            process.exit();
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
            // signalR doesn't seem to detect and reconnect - so we'll force it by restarting
            console.log('restarting');
            process.exit();
        };
    }

    updateIn(1);
    start();
};