// test file used to experiement with GPIOs and sound and stuff 

var Gpio = require('onoff').Gpio, // Constructor function for Gpio objects.
sleep = require('sleep'),
  iv;

var leds = [
  new Gpio(117, 'out'),      // Export GPIO #14 as an output.
  new Gpio(115, 'out'),      // Export GPIO #14 as an output.
  new Gpio(111, 'out'),      // Export GPIO #14 as an output.
  new Gpio(110, 'out')      // Export GPIO #14 as an output.
]

function doBeep(hz, duration) {
	var led = leds[3];
	var d = Math.floor( 1000000 / hz);
	var c = duration * hz;
	for(var i=0; i<c; i++) {
		led.writeSync(0);
		sleep.usleep( d );
		led.writeSync(1);
		sleep.usleep( d );
	}	
}

function doRange(start, end, duration) {
	var led = leds[0];
	var d1 = Math.floor( 1000000 / start);
	var d2 = Math.floor( 1000000 / end);
	var c = duration * (start + end) / 2;
	for (var i=0; i < c; i++) {
		var d = Math.floor((i/c) * d2 + (1-(i/c)) * d1);
		led.writeSync(0);
		sleep.usleep(d);
		led.writeSync(1);
		sleep.usleep(d);
	}
}

//doRange(1046, 1567.98, 1);
//return;

doBeep(1046 , 0.02);
sleep.usleep(1000000 * 0.01);
doBeep(1318 , 0.04);
return;

//var inc = Math.pow(2, 1/12);
//for(var i= 440; i< 16000; i *= inc) {
//	console.log(i);
//	doBeep(i, .05);
//	sleep.usleep( 1000000 * 0.5);
//}
//
//return;

var i = 0;
// Toggle the state of the LED on GPIO #14 every 200ms.
// Here synchronous methods are used. Asynchronous methods are also available.
iv = setInterval(function () {
var led = leds[i%4];
i++;
  led.writeSync(led.readSync() ^ 1); // 1 = on, 0 = off :)
}, 100);
