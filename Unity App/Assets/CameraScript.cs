using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class CameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    private GameObject MainCamera;
    private SerialPort sp;
    private Vector3 heading;
    private List<GameObject> listOfGameObjects;
    private readonly int sweepSize=36;
    private Rigidbody rb;
    private LineRenderer lr;
    void Start()
    {
        string portname = "COM4";
        sp = new SerialPort(portname, 9600);
        heading = new Vector3(0, 0, 0);
        listOfGameObjects = new List<GameObject>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        List<float[]> inputData = new List<float[]>();
        List<GameObject> refinedPoints = new List<GameObject>();

        float input = Input.GetAxis("Horizontal");
        if (sp.IsOpen)
        {
            if (input != 0.0f)
            {
                sp.Write("s");
                sp.DiscardInBuffer();
                sp.BaseStream.Flush();
                for (int i = 0; i < sweepSize; i += 1) {
                    float x = float.Parse(sp.ReadLine());
                    float y = float.Parse(sp.ReadLine());
                    float[] xy = { x, y };
                    inputData.Add(xy);
                }
                foreach (float[] f in inputData) {
                    Debug.Log(f[0]+", "+f[1]);
                    if (f[1] > 3)
                    {
                        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        block.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                        block.transform.position = new Vector3(f[0], 0, f[1])+rb.position;
                        block.GetComponent<Renderer>().material.color = Color.black;
                        listOfGameObjects.Add(block);
                    }
                }
                Debug.Log("List Count: " + listOfGameObjects.Count);
                
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
                        print(refinedPoints.Count);
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
                Vector3 start = refinedPoints[0].transform.position;
                Vector3 end = refinedPoints[refinedPoints.Count-1].transform.position;
                Vector3 wallDirection = start - end;
                float mag = wallDirection.magnitude;
                Vector3 direction = wallDirection/ mag;
                float angle = Vector3.SignedAngle(heading, direction,Vector3.up);
                float globalAngle;
                string command;
                if (angle < 3)
                {
                    sp.Write("f:30");
                    globalAngle = Vector3.SignedAngle(heading, Vector3.zero, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position+new Vector3(deltaX,0,deltaY));
                }
                else if (angle < 0)
                {
                    command = "l:" + angle.ToString();
                    sp.Write(command);
                    sp.Write("f:30");
                    globalAngle = Vector3.SignedAngle(direction, Vector3.zero, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position + new Vector3(deltaX, 0, deltaY));
                }
                else {
                    command = "r:" + angle.ToString();
                    sp.Write(command);
                    sp.Write("f:30");
                    globalAngle = Vector3.SignedAngle(direction, Vector3.zero, Vector3.up);
                    float deltaX = Mathf.Sin(globalAngle * Mathf.PI / 180) * 30;
                    float deltaY = Mathf.Cos(globalAngle * Mathf.PI / 180) * 30;
                    rb.MovePosition(rb.position + new Vector3(deltaX, 0, deltaY));
                }
                

                

            }
            //print("it works");
        }
        else {
            //print("nope");
            sp.Open();
        }
        
        
        
    }
}
