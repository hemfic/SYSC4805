#include <Ultrasonic.h>
#include <Servo.h>

int echo_pin = 10;
int trig_pin = 11;

Servo servo;
Ultrasonic ultrasonic(trig_pin, echo_pin);

void setup() {
  Serial.begin(9600);
  servo.attach(9);
}

void loop() {
  int i;
  // Reset servo position and wait for it to catch up.
  servo.write(i=180);
  delay(1000);

  // Sweep the sensor at 5 degrees per tick.
  while(i>0)
  {
    ping(i);
    i-=5;
  }
}

/*
 * Description: triggers a sensor ping at the given angle
 *              and returns the x and y coordinates of
 *              the sensed distance with the sensor as the
 *              origin.
 *              
 * Parameters:  angle - the angle of the sensor (0-180; left-right)
 * 
 * Returns:     an array of double containing a single x and y
 *              coordinate averaged over 10 samples.
 */
double* ping(int angle)
{
  float sampleTotal = 0;
  float distance_cm;
  double coordinates[2];
  long microsec;

  // Set servo position and give time for it to catch up.
  servo.write(angle);
  delay(200);
  
  for(int i=0; i<9; i+=1)
  {
    microsec = ultrasonic.timing();
    sampleTotal = ultrasonic.CalcDistance(microsec,Ultrasonic::CM);
    delay(5);
  }

  distance_cm = sampleTotal/10;
  
  // Make angles to the left of the sensor yield positive x positions.
  if(angle > 90)
  {
    angle += 90;
  }

  // x coordinate
  coordinates[0] = cos(angle*180/M_PI)*distance_cm;
  // y coordinate
  coordinates[1] = abs(sin(angle*180/M_PI)*distance_cm);

  return coordinates;
}
