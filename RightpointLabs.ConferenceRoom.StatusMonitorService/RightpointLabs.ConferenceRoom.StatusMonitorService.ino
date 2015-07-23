void setup() {
  // turn on the serial port
  Serial.begin(9600);
}

void loop() {
  // if we have data available (two bytes), process it
  //  byte 1 == port number + 32
  //  byte 2 == PWM value to write (<= 32 == 0, >= 230 == 1, middle == PWM if using an output that supports it)
  while(Serial.available() > 1) {
    int pin = Serial.read() - 32;
    int value = Serial.read();
    Serial.print(pin, DEC);
    Serial.println(value, DEC);
    if(value <= 32) {
      digitalWrite(pin, LOW);
    } else if(value >= 230) {
      digitalWrite(pin, HIGH);
    } else {
      analogWrite(pin, value);
    }
  }
}
