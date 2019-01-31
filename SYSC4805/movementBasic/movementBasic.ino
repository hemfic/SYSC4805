#include <SoftwareSerial.h>

SoftwareSerial Genotronex(3, 2); // TX,RX
int data; //data read from BT

const int AIA = 10;
const int AIB = 9;
const int BIA = 5;
const int BIB = 6;
byte speed = 100;

void setup() {
  Genotronex.begin(9600);
  Genotronex.println("Bluetooth On press f for forward, b for backward, r for right, l for left");
  pinMode(AIA, OUTPUT);
  pinMode(AIB, OUTPUT);
  pinMode(BIA, OUTPUT);
  pinMode(BIB, OUTPUT);
}
void loop() {
  if(Genotronex.available()){
    data=Genotronex.read();
    if(data=='f'){
      forward(1000);  
    }else if(data=='b'){
      backward(1000);
    }else if(data=='l'){
      pivotLeft(1000);
    }else if(data=='r'){
      pivotRight(1000);
    } 
  }
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
