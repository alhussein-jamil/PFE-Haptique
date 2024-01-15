using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arrow_movement : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    private Vector3 positionInitiale;

    float duration_of_wave = 3.0f;
    float height_of_curve = 0.5f; // Adjust this value to control the height of the parabolic curve
    float rotationSpeed = 0.13f;

    private float timer = 0.0f;
    private bool movingForward = false;
    private bool curvedMovement = false;

    void Start()
    {
        positionInitiale = transform.position;
    }


    private void Update()
    {
        if (movingForward)
        {
            timer += Time.deltaTime;

            float t = timer / duration_of_wave;
            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);
        }
        if (curvedMovement)
        {
            timer += Time.deltaTime;

            float t = timer / duration_of_wave;
            float yOffset = height_of_curve *((t - 1) + (t - 1)*(t - 1)) ;
             transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t) + new Vector3(0, yOffset, 0);
             transform.Rotate(Vector3.forward * rotationSpeed);
        }
        
        

        if (timer >= duration_of_wave)
        {
            timer = 0.0f;
            movingForward = false;
            curvedMovement = false;
            transform.position = positionInitiale;
            transform.rotation = Quaternion.Euler(0.0f , -90.0f , 0.0f);
        }
    }

    public void StartForwardMovement()
    {

        
        movingForward = true;
    }

    public void StartCurvedMovement()
    {

        transform.rotation = Quaternion.Euler(0.0f, -90.0f, -40.0f);
        curvedMovement = true;
    }

    public void UpdateSpeed(float visualSpeed)
    {
        duration_of_wave = visualSpeed / 6;
    }
}