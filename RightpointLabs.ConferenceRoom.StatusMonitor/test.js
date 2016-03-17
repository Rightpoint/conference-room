var configFile = path.join(__dirname, 'config.json');
console.log('Loading configuration from ' + configFile);
var config = JSON.parse(fs.readFileSync(configFile));

var LedManager = require('./ledManager.js');
var led = new LedManager(config);

// test colors
led.setColor(0, 0, 0, 0);
led.setColor(1, 0, 0, 1000);
setTimeout(function() {
    led.setColor(0, 1, 0, 1000);
    setTimeout(function() {
        led.setColor(0, 0, 1, 1000);
        setTimeout(function() {
            led.setCycle([ 
                { state: { red: 1, green: 0, blue: 0 }, duration: 1000 }, 
                { state: { red: 1, green: 0, blue: 0 }, duration: 5000 }, 
                { state: { red: 0.625, green: 0.125, blue: 0.9375 }, duration: 1000 },
                { state: { red: 1, green: 0, blue: 0 }, duration: 1000 }, 
                { state: { red: 1, green: 0, blue: 0 }, duration: 5000 }, 
                { state: { red: 1, green: 0.5, blue: 0 }, duration: 1000 }
            ]);
        }, 700);
    }, 700);
}, 700);
