using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brush_Movement : MonoBehaviour{

    public Transform startPoint; 
    public Transform First_vibror; 
    public Transform Last_vibror; 
    public Transform endPoint;

    private Vector3 positionInitiale;

    public float duration_of_wave;
    public float height_of_curve;

    private float timer = 0.0f;

    private bool First_movement = false;
    private bool curvedMovement = false;
    private bool Last_movement = false;

    void Start()
    {
        positionInitiale = transform.position;
    }

    private void Update()
    {
        // if the button on the Unity scrin is clicked then move toward the finish position

        

        
            if(First_movement){
            timer += Time.deltaTime;

            float t = timer / duration_of_wave;
            float yOffset = height_of_curve - height_of_curve *4*((t-0.5f)*(t-0.5f)) ;
            transform.position = Vector3.Lerp(startPoint.position, First_vibror.position, t) - new Vector3(0, yOffset, 0);

            if (timer >= duration_of_wave){
        
                timer = 0.0f;
                First_movement=false;
                curvedMovement=true;}
            }
       

            if(curvedMovement){
                timer += Time.deltaTime;
                
                float t = timer / duration_of_wave; //calculate the speed of the movement from startPoint to finishPoint based on the total duration of the movement is seconds 

                transform.position = Vector3.Lerp(First_vibror.position, Last_vibror.position, t);

                if (timer >= duration_of_wave){
        
                timer = 0.0f;
                curvedMovement=false;
                Last_movement=true;}


                }


            if(Last_movement){
                timer += Time.deltaTime;
                
                float t = timer / duration_of_wave;
                float yOffset = height_of_curve - height_of_curve *4*((t-0.5f)*(t-0.5f)) ;
                transform.position = Vector3.Lerp(Last_vibror.position, endPoint.position, t) - new Vector3(0, yOffset, 0);

            if (timer >= duration_of_wave){
        
                timer = 0.0f;
                Last_movement=false;
                }
            }

        

            
        }

        //when the brush is in the correct place turned of the movement and reset the timer 
        
    

    public void StartMovement()
    {
        transform.rotation = Quaternion.Euler(0.0f, 90.0f, -60.0f);
        First_movement = true;
    }

    // the information about the speed of the brush movement is read from the .csv file during the experiment
    public void UpdateSpeed(float visualSpeed)
    {
        duration_of_wave = visualSpeed/6;

    }


}