#include <Ultrasonic.h>
#include <Servo.h>
#include <SoftwareSerial.h>

Servo servo;
SoftwareSerial BTSerial(3, 2); // TX,RX
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
  BTSerial.begin(9600);
  BTSerial.flush();
  pinMode(AIA, OUTPUT);
  pinMode(AIB, OUTPUT);
  pinMode(BIA, OUTPUT);
  pinMode(BIB, OUTPUT);
  servo.attach(11);
}
void loop() {
  String params="";
  char trimmed[10];
  int command;
  char temp[10];
  char data;
  int param=0;
  int charsAvailable=BTSerial.available();
  if(charsAvailable>0){
  //if(Serial.available()>0){
    //command=Serial.readStringUntil(':');
    command = BTSerial.read();
    
    if((command!=115||command==102||command==98||command==108||command==114)&&command!=255&&command!=254){
      Serial.print("cmd: ");
      Serial.println(command);
      data=BTSerial.read();
      
      while(data!=59){
        data=BTSerial.read();
        if(data!=255&&data!=254){
          params.concat(data);
        }
        Serial.print("params: ");
        Serial.println(params);
      }
      params.toCharArray(temp,10);
      param=atoi(temp);
      Serial.println(param);
      if(command==102){
       forward(param);
      }
      else if(command==98){
        backward(param);
      }
      else if(command==108){
        pivotLeft(param);
      }
      else if(command==114){
        pivotRight(param);
      }
    }
    else if(command==115){
        sweep();
      }
    
  }else{
    stopDriving();
  }
  
}

void distance(){
  double distance_cm;
  double dummy=4;
  long microsec;
  
  microsec = ultrasonic.timing();
  distance_cm= ultrasonic.CalcDistance(microsec,Ultrasonic::CM);
  BTSerial.println(distance_cm);
  BTSerial.println(dummy);
  
}

void sweep(){
  int i;
  double xy[2];
  // Reset servo position and wait for it to catch up.
  i=180;
  servo.write(i);
  delay(1000);

  // Sweep the sensor at 5 degrees per tick.
  while(i>=0)
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
  boolean left;

  // Set servo position and give time for it to catch up.
  servo.write(angle);
  delay(600);
  
  
    microsec = ultrasonic.timing();
    sampleTotal += ultrasonic.CalcDistance(microsec,Ultrasonic::CM);
    delay(100);
  
  
  distance_cm = sampleTotal;
//  Serial.println(distance_cm);
  // Make angles to the left of the sensor yield positive x positions.
  if(angle >= 90)
  { left=true;
    angle -= 90;
  }
  else
  { left=false;
    angle = 90 - angle;
  }
  //Serial.println(angle);
  // x coordinate
  coordinates[0] = sin((angle*M_PI)/180)*distance_cm;
  if(left){
    coordinates[0]=coordinates[0]*-1;  
  }
  //Serial.println(coordinates[0]);
  BTSerial.println(coordinates[0]);
  // y coordinate
  coordinates[1] = abs(cos((angle*M_PI)/180)*distance_cm);
  //Serial.println(coordinates[1]);
  BTSerial.println(coordinates[1]);
  return coordinates;
}

float sonic(){
  float cmdistance;
  long microsec = ultrasonic.timing();
  BTSerial.print("ms: ");
  BTSerial.println(microsec);
  cmdistance= ultrasonic.CalcDistance(microsec,Ultrasonic::CM);
  BTSerial.print("Distance cm:");
  BTSerial.println(cmdistance);
  return cmdistance;
}


void backward(int t)
{ analogWrite(AIA, 0);
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
void stopDriving(){
  analogWrite(AIA, 0);
  analogWrite(AIB, 0);
  analogWrite(BIA, 0);
  analogWrite(BIB, 0);
  }
