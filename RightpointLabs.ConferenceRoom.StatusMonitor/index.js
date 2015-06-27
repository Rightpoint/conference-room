var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var Gpio = require('onoff').Gpio;
var async = require('async');
var signalR = require('signalr-client');
var path = require('path');
var Gpio = require('onoff').Gpio,
	sleep = require('sleep');

var configFile = path.join(__dirname, 'config.json');
console.log('Loading configuration from ' + configFile);
var config = JSON.parse(fs.readFileSync(configFile));

var redLed = new Gpio(config.redPin, 'out');
var greenLed = new Gpio(config.greenPin, 'out');
var speakerPin = new Gpio(config.speakerPin, 'out');

setPins(false, false, false);
doBeep(speakerPin, 1046, 0.04); // High C
sleep.usleep(1000000 * 0.01);
doBeep(speakerPin, 1046, 0.04); // High C
sleep.usleep(1000000 * 0.01);
doBeep(speakerPin, 1318.51, 0.08); // E
sleep.usleep(1000000 * 0.01);
doBeep(speakerPin, 1318.51, 0.08); // E

// test red
setPins(true, false, false);
sleep.usleep(1000000 * 0.3);

// test green
setPins(false, true, false);
sleep.usleep(1000000 * 0.3);

// and start
setPins(false, false, false);
updateIn(1);

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
    setPins(true, false, true);
}
function green() {
    console.log('green');
    setPins(false, true, true);
}
function doBeep(pin, hz, duration) {
	var d = Math.floor( 1000000 / hz);
	var c = Math.floor((duration * hz) / 2);
	for(var i=0; i<c; i++) {
		pin.writeSync(0);
		sleep.usleep( d );
		pin.writeSync(1);
		sleep.usleep( d );
	}	
}
function setPins(red, green, canBeep) {
	var beep = canBeep && (redLed.readSync() != red || greenLed.readSync() != green);
	redLed.writeSync(red ? 1 : 0);
	greenLed.writeSync(green ? 1 : 0);
	console.log('set pins');
	if(beep)
	{
		doBeep(speakerPin, 1046, 0.04); // High C
		sleep.usleep(1000000 * 0.01);
		doBeep(speakerPin, 1318.51, 0.08); // E
	}
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
