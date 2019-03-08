using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class CameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    SerialPort sp;
    int sweepSize=36;
    void Start()
    {
        string portname = "COM4";
        sp = new SerialPort(portname, 9600);
        
    }

    // Update is called once per frame
    void Update()
    {
        List<float[]> inputData = new List<float[]>();

        float input = Input.GetAxis("Horizontal");
        if (sp.IsOpen)
        {
            if (input != 0.0f)
            {
                sp.Write("s");
                for (int i = 0; i < sweepSize; i += 1) {
                    float x = float.Parse(sp.ReadLine());
                    float y = float.Parse(sp.ReadLine());
                    float[] xy = { x, y };
                    inputData.Add(xy);
                }
                foreach (float[] f in inputData) {
                    print(f[0]);
                    print(f[1]);
                    GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.localScale = new Vector3(0.2f, 1, 0.2f);
                    block.transform.position = new Vector3(f[0], 0, f[1]);
                    block.GetComponent<Renderer>().material.color = Color.black;
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
