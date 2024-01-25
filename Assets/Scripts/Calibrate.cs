using System;
using UnityEngine;
using UnityEngine.UI;

public class Calibrate : MonoBehaviour
{
    public Transform RightHand;
    public Transform LeftHand;
    public Transform CenterEye;
    public GameObject CountdownToDisable;
    public Text CountdownText;
    public Text CalibrationMessage;
    public bool CalibrateOnStart = false;
    private bool calibrated = false;
    private float countdownTimer = 5f;

    void Start()
    {
        CalibrationMessage.gameObject.SetActive(false);
        if (CalibrateOnStart)
        {
            StartCalibration();
        }
    }

    public void CalibratePosition()
    {
        transform.position = new Vector3(transform.position.x, (RightHand.position.y + LeftHand.position.y) / 2, transform.position.z);

        double rotationAngleZ = Math.Atan2(RightHand.position.y - LeftHand.position.y, RightHand.position.x - LeftHand.position.x);
        rotationAngleZ = rotationAngleZ * 180 / Math.PI;

        Quaternion handRotation = Quaternion.Euler(0, 0, (float)rotationAngleZ);
        Quaternion eyeRotation = Quaternion.Euler(0, CenterEye.rotation.eulerAngles.y, 0);
        transform.rotation = handRotation * eyeRotation;
    }

    public void StartCalibration()
    {
        if (!calibrated)
        {
            CalibrationMessage.gameObject.SetActive(true);
            CalibrationMessage.text = "Calibration started. Please place your right hand on the table.";
            countdownTimer = 5f;
            if (CountdownToDisable != null)
            {
                CountdownToDisable.SetActive(true);
            }
        }
    }

    void Update()
    {
        if (!calibrated)
        {
            if (countdownTimer > 0)
            {
                countdownTimer -= Time.deltaTime;
                int seconds = Mathf.CeilToInt(countdownTimer);
                CountdownText.text = seconds.ToString();
            }
            else
            {
                calibrated = true;
                CalibratePosition();
                if (CountdownToDisable != null)
                {
                    CountdownToDisable.SetActive(false);
                }
                CalibrationMessage.gameObject.SetActive(false);
            }
        }
    }

    public void RestartScript()
    {
        // Resetting the state of the script.
        calibrated = false;
        countdownTimer = 5f;
        CalibrationMessage.gameObject.SetActive(false);
        CalibrateOnStart = true;
        if (CalibrateOnStart)
        {
            StartCalibration();
        }
    }
}
