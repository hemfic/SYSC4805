#include <Ultrasonic.h>
#include <Servo.h>

#include <SoftwareSerial.h>

Servo servo;
SoftwareSerial Genotronex(3, 2); // TX,RX
int data; //data read from BT
int servoPin=11;
int trig=13;
int echo=12;

Ultrasonic ultrasonic(trig,echo);

const int AIA = 10;
const int AIB = 9;
const int BIA = 5;
const int BIB = 6;
byte speed = 100;

void setup() {
  Serial.begin(9600);
  Genotronex.begin(9600);
  Genotronex.println("Bluetooth On press f for forward, b for backward, r for right, l for left");
  pinMode(AIA, OUTPUT);
  pinMode(AIB, OUTPUT);
  pinMode(BIA, OUTPUT);
  pinMode(BIB, OUTPUT);
  servo.attach(11);
}
void loop() {
  int data;
  
  if(Genotronex.available()){
    data = Genotronex.read();
    
    Serial.println(data);
    if(data==102){
      Genotronex.println("hello");
      forward(100);
    }
    else if(data==98){
      backward(100);
    }
    else if(data==108){
      pivotLeft(100);
    }
    else if(data==114){
      pivotRight(10);
    }
    else if(data==115){
      sweep();
    }
  }
  
}

void sweep(){
  int i;
  double xy[2];
  // Reset servo position and wait for it to catch up.
  servo.write(i=180);
  delay(1000);

  // Sweep the sensor at 5 degrees per tick.
  while(i>0)
  {
    ping(i,xy);
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
double* ping(int angle,double coordinates[])
{
  float sampleTotal = 0;
  float distance_cm;
  long microsec;

  // Set servo position and give time for it to catch up.
  servo.write(angle);
  delay(200);
  
  for(int i=0; i<9; i+=1)
  {
    microsec = ultrasonic.timing();
    sampleTotal += ultrasonic.CalcDistance(microsec,Ultrasonic::CM);
    delay(5);
  }

  distance_cm = sampleTotal/10;
//  Serial.println(distance_cm);
  // Make angles to the left of the sensor yield positive x positions.
  if(angle >= 90)
  {
    angle -= 90;
  }
  else
  {
    angle = 90 - angle;
  }

  // x coordinate
  coordinates[0] = sin(angle*180/M_PI)*distance_cm;
  Genotronex.print("X=");
  Genotronex.println(coordinates[0]);
  // y coordinate
  coordinates[1] = abs(cos(angle*180/M_PI)*distance_cm);
  Genotronex.print("Y=");
  Genotronex.println(coordinates[1]);
  return coordinates;
}

float sonic(){
  float cmdistance;
  long microsec = ultrasonic.timing();
  Genotronex.print("ms: ");
  Genotronex.println(microsec);
  cmdistance= ultrasonic.CalcDistance(microsec,Ultrasonic::CM);
  Genotronex.print("Distance cm:");
  Genotronex.println(cmdistance);
  return cmdistance;
}


void backward(int t)
{
  analogWrite(AIA, 0);
  analogWrite(AIB, speed);
  analogWrite(BIA, 0);
  analogWrite(BIB, speed+25);
  delay(t);
}
void forward(int t)
{
  analogWrite(AIA, speed);
  analogWrite(AIB, 0);
  analogWrite(BIA, speed+25);
  analogWrite(BIB, 0);
  delay(t);
}
void pivotRight(int t)
{
  analogWrite(AIA, speed);
  analogWrite(AIB, 0);
  analogWrite(BIA, 0);
  analogWrite(BIB, speed+25);
  delay(t);
}
void pivotLeft(int t)
{
  analogWrite(AIA, 0);
  analogWrite(AIB, speed);
  analogWrite(BIA, speed+25);
  analogWrite(BIB, 0);
  delay(t);
}
