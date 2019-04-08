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
        List<GameObject> refinedPoints = new List<GameObject>();   //List of refined points after each iteration of removing outliers
        List<GameObject> regressedPoints = new List<GameObject>();

        float input = Input.GetAxis("Jump");
        if (sp.IsOpen)
        {   //If space bar is pressed
            if (input != 0.0f)
            {
                sp.Write("s");

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
                    //Eliminates noise; if a point within the list of inputData is to close relative to the robot
                    if (f[1] > 3 && f[1]<100 && f[0]<100)
                    {
                        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        block.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                        block.transform.position = new Vector3(f[0], 0, f[1])+rb.position;
                        block.GetComponent<Renderer>().material.color = Color.black;
                        listOfGameObjects.Add(block);
                    }
                }
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
                    }
                }

                for (int i = 0; i < refinedPoints.Count; i++)
                {
                    refinedPoints[i].GetComponent<Renderer>().material.color = Color.blue;
                }

                // Fit regression lines. Sliding window is 3
                int slidingWindowSize = 3;

                for (int i = 0; i < refinedPoints.Count - slidingWindowSize; i++) {
                    float xSum = 0.0f;
                    float zSum = 0.0f;
                    float xzSum = 0.0f;
                    float xSquaredSum = 0.0f;
                    float minX = refinedPoints[i].transform.position.x;
                    float maxX = refinedPoints[i].transform.position.x;

                    for (int j = i; j < i + slidingWindowSize; j++)
                    {
                        Debug.Log("X: " + refinedPoints[j].transform.position.x + " Z: " + refinedPoints[j].transform.position.z);
                        xSum += refinedPoints[j].transform.position.x;
                        zSum += refinedPoints[j].transform.position.z;
                        xzSum += refinedPoints[j].transform.position.x * refinedPoints[j].transform.position.z;
                        xSquaredSum += refinedPoints[j].transform.position.x * refinedPoints[j].transform.position.x;

                        if (refinedPoints[j].transform.position.x < minX)
                        {
                            minX = refinedPoints[j].transform.position.x;
                        }

                        if (refinedPoints[j].transform.position.x > maxX)
                        {
                            maxX = refinedPoints[j].transform.position.x;
                        }
                    }
                    
                    float xMean = xSum / 3;
                    float zMean = zSum / 3;
                    float xzMean = xzSum / 3;
                    float xSquaredMean = xSquaredSum / 3;

                    float slope = ((xMean * zMean) - xzMean) / ((xMean * xMean) - xSquaredMean);
                    float constant = zMean - (slope * xMean);

                    Debug.Log("Min: " + minX + " Max: " + maxX + " Slope: " + slope);

                    GameObject startBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    startBlock.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                    startBlock.transform.position = new Vector3(minX, 0, ((slope * minX) + constant));
                    startBlock.GetComponent<Renderer>().material.color = Color.yellow;

                    GameObject endBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    endBlock.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                    endBlock.transform.position = new Vector3(maxX, 0, ((slope * maxX) + constant));
                    endBlock.GetComponent<Renderer>().material.color = Color.red;

                    lr = startBlock.AddComponent<LineRenderer>();
                    lr.material.color = Color.green;
                    lr.startWidth = 0.5f;
                    lr.endWidth = 0.5f;
                    lr.SetPosition(0, startBlock.transform.position);
                    lr.SetPosition(1, endBlock.transform.position);
                    regressedPoints.Add(startBlock);
                    regressedPoints.Add(endBlock);
                }

                //Merge regression lines by starting and stopping each line at points of intersection with other lines.
                for (int i = 0; i <= regressedPoints.Count - 4; i += 2)
                {
                    GameObject line1StartObject = regressedPoints[i];
                    GameObject line1EndObject = regressedPoints[i + 1];
                    for (int j = i + 2; j <= regressedPoints.Count - 2; j += 2)
                    {
                        GameObject line2StartObject = regressedPoints[j];
                        GameObject line2EndObject = regressedPoints[j + 1];
                        float xIntersect = 0.0f;
                        float zIntersect = 0.0f;
                        if (checkForIntersection(line1StartObject.transform.position.x, line1StartObject.transform.position.z, line1EndObject.transform.position.x, line1EndObject.transform.position.z, line2StartObject.transform.position.x, line2StartObject.transform.position.z, line2EndObject.transform.position.x, line2EndObject.transform.position.z, out xIntersect, out zIntersect))
                        {
                            Debug.Log("XIntersect: " + xIntersect + " ZIntersext: " + zIntersect);
                            LineRenderer line1Renderer = line1StartObject.GetComponent<LineRenderer>();
                            LineRenderer line2Renderer = line2StartObject.GetComponent<LineRenderer>();
                            line1Renderer.SetPosition(1, new Vector3(xIntersect, 0, zIntersect));
                            line2Renderer.SetPosition(0, new Vector3(xIntersect, 0, zIntersect));
                        }
                    }
                }
		/*  Navigation portion: Determines where the robot should travel to next based on currently scanned points
                    Estimates a general direction for the list of points based off the 1st point and the last point of the refined points.
                    Normalizes the direction of refined points and checks the angle between the normalized direction and the current 
                    heading of the robot.
                */
                int midIndex = 0;
                for (int i = 0; i < refinedPoints.Count; i += 1) {
                    if (refinedPoints[midIndex].transform.position.x - rb.position.x > refinedPoints[i].transform.position.x-rb.position.x) {
                        if (refinedPoints[midIndex].transform.position.z - rb.position.z > refinedPoints[i].transform.position.z - rb.position.z) {
                            midIndex = i;
                        }

                    }
                        
                }
                Vector3 start;
                Vector3 end;
                if (midIndex < 5)
                {
                    start = refinedPoints[0].transform.position;
                }
                else {
                    start = refinedPoints[midIndex - 5].transform.position;
                }
                if (refinedPoints.Count-1-midIndex < 5)
                {
                    end = refinedPoints[refinedPoints.Count-1].transform.position;
                }
                else
                {
                    end = refinedPoints[midIndex + 5].transform.position;
                }
                Vector3 wallDirection = start - end;
                float mag = wallDirection.magnitude;
                Vector3 direction = wallDirection/ mag;
                float angle = Vector3.SignedAngle(heading, direction,Vector3.up);       //Used as correction angle to determine correct heading and is sent to arduino as a parameter for the turn
                float globalAngle;
                string command;
                byte[] sendData;
		//If the angle is within a 3 degree threshold then send message to arduino to proceed straight 30 cm 
                if (Mathf.Abs(angle) < 3)
                {
                    command = "f:650;";
                    sendData = System.Text.Encoding.ASCII.GetBytes(command);
                    sp.Write(sendData,0,sendData.Length);
                    globalAngle = Vector3.SignedAngle(heading, Vector3.left, Vector3.up);
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
                    heading = direction;
                    globalAngle = Vector3.SignedAngle(direction, Vector3.left, Vector3.up);
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
                    globalAngle = Vector3.SignedAngle(direction, Vector3.left, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position + new Vector3(deltaY, 0, deltaX));
                }
                

                

            }
        }
        else {
            sp.Open();
        }
        
        
        
    }

    public static bool checkForIntersection(float p0_x, float p0_y, float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, out float i_x, out float i_y)
    {
        i_x = 0.0f;
        i_y = 0.0f;
        float s1_x, s1_y, s2_x, s2_y;
        s1_x = p1_x - p0_x; s1_y = p1_y - p0_y;
        s2_x = p3_x - p2_x; s2_y = p3_y - p2_y;
        float s, t;
        s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
        t = (s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            // Collision detected
            i_x = p0_x + (t * s1_x); //Intersection point x
            i_y = p0_y + (t * s1_y); //Intersection point y
            return true;
        }
        return false; // No collision      
    }

}
