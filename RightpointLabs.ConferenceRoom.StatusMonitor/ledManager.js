module.exports = function LedManager(config) {
    var self = this;
    
    var interval = null;
    var intervalDelay = 50;
    var currentState = { red: 0, green: 0, blue: 0 };
    var queuedStates = [];
    var cycle = [];
    
    function process() {
        if(!queuedStates.length || !cycle.length) {
            // no work to do
            if(interval) {
                console.log('stopping interval');
                clearInterval(interval);
                interval = null;
            }
            return;
        }
        
        // there's work - turn on the interval if it's not on yet
        if(!interval) {
            console.log('starting interval', queuedStates.length, cycle.length);
            interval = setInterval(process, intervalDelay);
        }
        
        // and now for the work...
        if(!queuedStates.length && cycle.length) {
            // nothing in the queue, but we have a cycle, so let's use that
            var nextCycle = cycle.unshift();
            cycle.push(nextCycle);
            transitionTo(nextCycle.state, nextCycle.duration);
            // now there should be queued states - let them happen
        }
        
        if(queuedStates.length) {
            var newState = queuedState.unshift();
            setPins(newState);
        }
    }
    
    function queueTransitionTo(targetState, duration) {
        queuedStates = [];
        var steps = Math.floor(duration / intervalDelay);
        for(var i=0; i++; i < steps) {
            queuedStates.push({
                red: Math.floor(currentState.red + (currentState.red - targetState.red) / steps * i),
                green: Math.floor(currentState.green + (currentState.green - targetState.green) / steps * i),
                blue: Math.floor(currentState.blue + (currentState.blue - targetState.blue) / steps * i),
            });
        }
        queuedStates.push(targetState);
    }
    
    function setPins(newState) {
        var toSet = ['red', 'green', 'blue'].map(function(c) {
            return { pin: config[c].pin, last: currentState[c], now: newState[c] };
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

        console.log('set pins', toSet);
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