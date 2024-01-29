using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // Ajoutez cette ligne si vous utilisez l' l ment Text standard

public class Calibrate : MonoBehaviour
{
    public Transform RightHand;
    public Transform LeftHand;
    public GameObject robotRoot;
    public Text countdownText; // R f rence   l' l ment Text sur le Canvas




    public Vector3 initPosition;
    public Quaternion initRotation;

    void Start()
    {
        initPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        initRotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
    }
    void Reset()
    {
        transform.position = initPosition;
        transform.rotation = initRotation;
        robotRoot.GetComponent<ArticulationBody>().TeleportRoot(robotRoot.transform.position, robotRoot.transform.rotation);
    }
    public void CalibratePosition()
    {
        Reset();
        Vector3 newPosition = new Vector3((RightHand.position.x + LeftHand.position.x) / 2, (RightHand.position.y + LeftHand.position.y) / 2, (RightHand.position.z + LeftHand.position.z) / 2);
        transform.position = newPosition;

        double rotationAngle_y = System.Math.Atan2(RightHand.position.z - LeftHand.position.z, RightHand.position.x - LeftHand.position.x);
        rotationAngle_y *= 180 / System.Math.PI;
        // double rotationAngle_z = Math.Atan2(RightHand.position.y - LeftHand.position.y, RightHand.position.x - LeftHand.position.x); 
        // rotationAngle_z *= 180 / Math.PI;
        // transform.Rotate(0, -(float)rotationAngle_y, 0, Space.Self);
        transform.RotateAround(transform.position, Vector3.up, -(float)rotationAngle_y);
        robotRoot.GetComponent<ArticulationBody>().TeleportRoot(robotRoot.transform.position, robotRoot.transform.rotation);
     
    }

    // M thode pour d marrer la coroutine de calibration
    public void StartCalibration()
    {
        StartCoroutine(CalibrationRoutine());
    }

    // Coroutine pour attendre 5 secondes avant de calibrer
    IEnumerator CalibrationRoutine()
    {
        float remainingTime = 5f;
        while (remainingTime > 0f)
        {

            countdownText.text = "Calibration in " + Mathf.CeilToInt(remainingTime) + "s";
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        countdownText.text = "";
        CalibratePosition();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartCalibration();
        }
    }
}