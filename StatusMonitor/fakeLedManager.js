module.exports = function FakeLedManager(config) {
    this.setColor = function setColor(red, green, blue, duration) {
        console.log('set color: ', { r: red, g: green, b: blue, d: duration });
    };
    this.setCycle = function setCycle(newCycle) {
        console.log('set cycle: ', newCycle);
    };
}
