var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var path = require('path');

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
                start();
            }, 500);
        }, 500);
    }, 500);
}, 500);

function start() {
    var deviceKeyFile = path.join(__dirname, 'devicekey');
    if(!fs.existsSync(deviceKeyFile)) {
        new Promise(function(resolve, reject) {
            var options = url.parse(config.apiServer + "/device/create?organizationId=" + config.organizationId + "&joinKey=" + config.joinKey);
            options.method = "POST";
            var data = "";
            return http.request(options, function(e) {
                e.on('data', function(c) { data += String(c); });
                e.on('end', function() { resolve(data); });
            }).end();
        }).then(function(data) {
            if(!data) {
                process.exit(-1);
            }
            fs.writeFileSync(deviceKeyFile, data);
            execute();
        }, function() {
            console.log('error creating device', arguments);
            process.exit(-1);
        });
    } else {
        execute();
    }
}

function execute(){
    config.deviceKey = fs.readFileSync(deviceKeyFile);
    require('./execute')(config, led);
}
