var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var path = require('path');
var jwt_decode = require('jwt-decode');

var configFile = path.join(__dirname, 'config.json');
console.log('Loading configuration from ' + configFile);
var config = JSON.parse(fs.readFileSync(configFile));

var LedManager = (config.red || config.green || config.blue) ? require('./ledManager.js') : require('./fakeLedManager.js');
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

var deviceKeyFile = config.deviceKeyFile || path.join(__dirname, 'devicekey');
function start() {
    if(!fs.existsSync(deviceKeyFile)) {
        console.log('creating new device key for ' + deviceKeyFile);
        new Promise(function(resolve, reject) {
            var options = url.parse(config.apiServer + "/devices/create?organizationId=" + config.organizationId + "&joinKey=" + config.joinKey);
            options.method = "POST";
            var data = "";
            return http.request(options, function(e) {
                e.on('data', function(c) { data += String(c); });
                e.on('end', function() { resolve(data); });
            }).end();
        }).then(function(data) {
            if(!data) {
                console.log('got no data for device key from server');
                process.exit(-1);
            }
            if(!jwt_decode(data)) {
                console.log('unable to decode device key from server');
                process.exit(-1);
            }
            fs.writeFileSync(deviceKeyFile, data, "utf8");
            execute();
        }, function() {
            console.log('error creating device', arguments);
            process.exit(-1);
        });
    } else {
        console.log('using existing device key from ' + deviceKeyFile);
        execute();
    }
}

function execute(){
    config.deviceKey = fs.readFileSync(deviceKeyFile, "utf8");
    var e = require('./execute');
    e(config, led);
}
