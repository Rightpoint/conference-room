void setup() {
  // before we start up, let's put on a little light show
  digitalWrite(2, HIGH);
  delay(1000);
  digitalWrite(2, LOW);
  delay(100);
  digitalWrite(5, HIGH);
  delay(1000);
  digitalWrite(5, LOW);
  delay(100);
  digitalWrite(6, HIGH);
  delay(1000);
  digitalWrite(6, LOW);
  
  // turn on the serial port
  Serial.begin(9600);

  while(!Serial.available() > 0) {
    // while we wait for a connection - indicate we're bored
    digitalWrite(2, HIGH);
    delay(100);
    digitalWrite(2, LOW);
    delay(50);
    digitalWrite(5, HIGH);
    delay(100);
    digitalWrite(5, LOW);
    delay(50);
    digitalWrite(6, HIGH);
    delay(100);
    digitalWrite(6, LOW);
    delay(1650);
  }
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
