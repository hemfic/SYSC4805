using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class CameraScript : MonoBehaviour
{
    
    private GameObject MainCamera;                          //Main camera gameobject
    private SerialPort sp;                                  //serialport
    private Vector3 heading;                                //Direction of the physical robot
    private List<GameObject> listOfGameObjects;             //List of points scanned by the ultrasonic sensor
    private readonly int sweepSize=36;                      //Number of {x, y} pairs to be recieved
    private Rigidbody rb;                                   //Mechanic used to move the visual reference point
    private LineRenderer lr;                                //Used to create linear regression between set of scanned points

    //Start is called before the first frame update
    //Setup serial port for bluetooth connection, default direction of the physical robot
    void Start()
    {
        string portname = "COM4";                       //Portname changes on each bluetooth connection
        sp = new SerialPort(portname, 9600);
        heading = new Vector3(0, 0, 0);
        listOfGameObjects = new List<GameObject>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        List<float[]> inputData = new List<float[]>();
        List<GameObject> refinedPoints = new List<GameObject>();    //List of refined points after each iteration of removing outliers

        float input = Input.GetAxis("Jump");
        
        //If the serial port is open
        if (sp.IsOpen)
        {
            //If space bar is pressed
            if (input != 0.0f)
            {
                print("4");
                sp.Write("s");
                print("5");

                //Collects all data recieved by the bluetooth sensor and stores it within the inputData list
                for (int i = 0; i < sweepSize; i += 1) {
                    string inX = sp.ReadLine();
                    float x = float.Parse(inX);
                    float y = float.Parse(sp.ReadLine());
                    float[] xy = { x, y };
                    inputData.Add(xy);
                }

                //Creates a black cube for each acceptable point within the list of inputData
                foreach (float[] f in inputData) {
                    //Debug.Log(f[0]+", "+f[1]);
                    //Eliminates noise; if a point within the list of inputData is to close relative to the robot
                    if (f[1] > 3)
                    {
                        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        block.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                        block.transform.position = new Vector3(f[0], 0, f[1])+rb.position;
                        block.GetComponent<Renderer>().material.color = Color.black;
                        listOfGameObjects.Add(block);
                    }
                }
                //Debug.Log("List Count: " + listOfGameObjects.Count);
                
                //Compares a point with its neighbouring points with respect to time
                //If the displacement between a pair points is less than a threshold, then add the point to the list of refinedpoints
                for (int j = 0; j < listOfGameObjects.Count; j += 1)
                {
                    if (j > 0 && j < listOfGameObjects.Count - 1)
                    {
                        Vector3 location = listOfGameObjects[j].transform.position;
                        Vector3 previous = listOfGameObjects[j - 1].transform.position;
                        Vector3 next = listOfGameObjects[j + 1].transform.position;

                        if (Mathf.Abs(location.x - previous.x) < 5 && Mathf.Abs(location.z - previous.z) < 5)
                        {
                            if (Mathf.Abs(location.x - next.x) < 5 && Mathf.Abs(location.z - next.z) < 5)
                            {
                                refinedPoints.Add(listOfGameObjects[j]);
                                
                            }
                        }
                    //print(refinedPoints.Count);
                    }
                }
                /*
                Vector3 averageVel = Vector3.zero;
                for (int i = refinedPoints.Count - 2; i >= 0; i--)
                {
                    averageVel += refinedPoints[i + 1].transform.position - refinedPoints[i].transform.position;
                    lr = refinedPoints[i].AddComponent<LineRenderer>();

                    lr.SetPosition(0, refinedPoints[i].transform.position);
                    lr.SetPosition(1, refinedPoints[i + 1].transform.position);
                    lr.material.color = Color.green;
                    Debug.DrawLine(refinedPoints[i + 1].transform.position, refinedPoints[i].transform.position, Color.red);
                    Debug.Log("Line: " + refinedPoints[i + 1].transform.position + ", " + refinedPoints[i].transform.position);
                }
                averageVel /= refinedPoints.Count;
                averageVel.Normalize();

                Debug.DrawLine(transform.position, transform.position + averageVel * 5, Color.white);
                Debug.DrawLine(refinedPoints[0].transform.position, refinedPoints[refinedPoints.Count - 1].transform.position, Color.green);
                //refinedPoints.RemoveAt(0);*/
                print("1");
                /*  Navigation portion: Determines where the robot should travel to next based on currently scanned points
                    Estimates a general direction for the list of points based off the 1st point and the last point of the refined points.
                    Normalizes the direction of refined points and checks the angle between the normalized direction and the current 
                    heading of the robot.
                */
                Vector3 start = refinedPoints[0].transform.position;
                Vector3 end = refinedPoints[refinedPoints.Count-1].transform.position;
                Vector3 wallDirection = start - end;
                float mag = wallDirection.magnitude;
                Vector3 direction = wallDirection/ mag;
                float angle = Vector3.SignedAngle(heading, direction,Vector3.up);           //Used as correction angle to determine correct heading and is sent to arduino as a parameter for the turn
                float globalAngle;
                string command;
                byte[] sendData;
                print("2");
                //If the angle is within a 3 degree threshold then send message to arduino to proceed straight 30 cm 
                if (angle < 3)
                {
                    command = "f:300;";
                    sendData = System.Text.Encoding.ASCII.GetBytes(command);
                    sp.Write(sendData,0,sendData.Length);
                    globalAngle = Vector3.SignedAngle(heading, Vector3.zero, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position+new Vector3(deltaX,0,deltaY));
                }
                //If the angle is less than 0 degree threshold then send message to arduino to turn left and proceed straight 30 cm                 
                else if (angle < 0)
                {
                    command = "l:" + angle.ToString()+";";
                    sendData = System.Text.Encoding.ASCII.GetBytes(command);
                    sp.Write(sendData, 0, sendData.Length); command = "f:300;";
                    sendData = System.Text.Encoding.ASCII.GetBytes(command);
                    sp.Write(sendData, 0, sendData.Length);
                    globalAngle = Vector3.SignedAngle(direction, Vector3.zero, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position + new Vector3(deltaX, 0, deltaY));
                }
                //Otherwise send message to arduino to turn right and proceed straight 30 cm
                else {
                    command = "r:" + angle.ToString()+";";
                    sendData = System.Text.Encoding.ASCII.GetBytes(command);
                    sp.Write(sendData, 0, sendData.Length); command = "f:300;";
                    sendData = System.Text.Encoding.ASCII.GetBytes(command);
                    sp.Write(sendData, 0, sendData.Length);
                    globalAngle = Vector3.SignedAngle(direction, Vector3.zero, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position + new Vector3(deltaY, 0, deltaX));
                }
                print("3");
                

                

            }
            //print("it works");
        }
        else {
            //print("nope");
            sp.Open();
        }
        
        
        
    }
}
