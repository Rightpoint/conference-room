var pwm = require('pi-blaster.js');

module.exports = function LedManager(config) {
    var self = this;
    
    var debug = false;
    var interval = null;
    var intervalDelay = 50;
    var currentState = { red: 0, green: 0, blue: 0 };
    var queuedStates = [];
    var cycle = [];
    
    function process() {
        if(debug) console.log('process', queuedStates.length, cycle.length);
        if(!queuedStates.length && !cycle.length) {
            // no work to do
            if(interval) {
                if(debug) console.log('stopping interval');
                clearInterval(interval);
                interval = null;
            }
            return;
        }
        
        // there's work - turn on the interval if it's not on yet
        if(!interval) {
            if(debug) console.log('starting interval', queuedStates.length, cycle.length);
            interval = setInterval(process, intervalDelay);
        }
        
        // and now for the work...
        if(!queuedStates.length && cycle.length) {
            // nothing in the queue, but we have a cycle, so let's use that
            var nextCycle = cycle.shift();
            cycle.push(nextCycle);
            if(debug) console.log('next cycle', nextCycle);
            queueTransitionTo(nextCycle.state, nextCycle.duration);
            // now there should be queued states - let them happen
        }
        
        if(queuedStates.length) {
            var newState = queuedStates.shift();
            setPins(newState);
        } else {
            if(debug) console.log('odd, no work to do...');
        }
    }
    
    function queueTransitionTo(targetState, duration) {
        if(debug) console.log('prepping transition', currentState, targetState, duration);
        queuedStates = [];
        var steps = Math.floor(duration / intervalDelay);
        for(var i=0; i < steps; i++) {
            queuedStates.push({
                red: currentState.red + (targetState.red - currentState.red) / steps * i,
                green: currentState.green + (targetState.green - currentState.green) / steps * i,
                blue: currentState.blue + (targetState.blue - currentState.blue) / steps * i,
            });
        }
        queuedStates.push(targetState);
        if(debug) console.log('transition', duration, steps, queuedStates);
    }
    
    function setPins(newState) {
        if(debug) console.log('setPins', newState, currentState);
        var toSet = ['red', 'green', 'blue'].map(function(c) {
            return { pin: config[c].pin, last: currentState[c] * config[c].brightness, now: newState[c] * config[c].brightness };
        });
        
        // var toSet = [
        //     { pin: config.red.pin, last: currentState.red, now: red },
        //     { pin: config.green.pin, last: currentState.green, now: green },
        //     { pin: config.blue.pin, last: lastBlue, now: blue },
        // ];

        // make sure we set the ones with the largest decrease in power first, largest increase in power last (to avoid over-driving our power supply due to the transition)
        toSet.forEach(function(i) { i.delta = i.now - (i.last || 0); });
        toSet.sort(function(a,b) { return a.delta < b.delta ? -1 : a.delta > b.delta ? 1 : 0; });
        toSet.forEach(function(i) { pwm.setPwm(i.pin, i.now); });

        currentState = newState;

        if(debug) console.log('set pins', toSet);
    }
    
    this.setColor = function setColor(red, green, blue, duration) {
        cycle = [];
        queueTransitionTo({ red: red, green: green, blue: blue}, duration || 0);
        process();
    };
    this.setCycle = function setCycle(newCycle) {
        cycle = newCycle;
        queuedStates = [];
        process();
    };
}
