using System;
using UnityEngine;

public class calibrate2 : MonoBehaviour
{
    public Transform RightHand;
    public Transform LeftHand;
    public Transform CenterEye;
    public bool CalibrateOnStart = false;
    private float startTime;
    private bool calibrated = false;
    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }

    public void calibratePosition()
    {
        // Make it so that this object is sitting on the hands 
        transform.position = new Vector3(transform.position.x, (RightHand.position.y + LeftHand.position.y) / 2, transform.position.z);

        double rotationAngle_z = Math.Atan2(RightHand.position.y - LeftHand.position.y, RightHand.position.x - LeftHand.position.x);
        rotationAngle_z = rotationAngle_z * 180 / Math.PI;

        // set the rotation along z to be aligned with the two hands
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,transform.rotation.eulerAngles.y, (float) rotationAngle_z);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,CenterEye.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

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
