using System;
using System.Drawing.Drawing2D;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Oculus.Platform;
using Unity.VisualScripting;
using UnityEngine;

public class calibrate : MonoBehaviour
{
    public Transform RightHand;
    public Transform LeftHand;
    public bool CalibrateOnStart = false;
    private float startTime;
    private bool calibrated = false;
    public GameObject robotRoot;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }


    public void calibratePosition()
    {
        // Make it so that this object is sitting on the hands 
        Vector3 newPosition = new Vector3((RightHand.position.x + LeftHand.position.x) / 2, (RightHand.position.y + LeftHand.position.y) / 2, (RightHand.position.z + LeftHand.position.z) / 2);
        transform.position = newPosition;
        
        double rotationAngle_y = Math.Atan2(RightHand.position.z - LeftHand.position.z, RightHand.position.x - LeftHand.position.x);
        rotationAngle_y *= 180 / Math.PI;
        // double rotationAngle_z = Math.Atan2(RightHand.position.y - LeftHand.position.y, RightHand.position.x - LeftHand.position.x); 
        // rotationAngle_z *= 180 / Math.PI;
        // transform.Rotate(0, -(float)rotationAngle_y, 0, Space.Self);
        transform.RotateAround(transform.position, Vector3.up, -(float)rotationAngle_y);
        robotRoot.GetComponent<ArticulationBody>().TeleportRoot(robotRoot.transform.position, robotRoot.transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        if(CalibrateOnStart)
        if (!calibrated && Time.time - startTime > 5){
            calibrated = true;
            calibratePosition();
        }

    }
}
