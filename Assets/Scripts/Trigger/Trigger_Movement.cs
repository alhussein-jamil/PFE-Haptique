using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_Movement : MonoBehaviour{

    private Vector3 startPoint; 
    private Vector3 First_vibror; 
    private Vector3 Last_vibror; 
    private Vector3 endPoint;

    private Vector3 positionInitiale;

    public float duration_of_wave;
    public float speed;
    public float height_of_curve;

    private float timer = 0.0f;
    public float Time_factor;

    private bool First_movement = false;
    private bool StraightMovement = false;
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
            float Offset = height_of_curve - height_of_curve *4*((t-0.5f)*(t-0.5f)) ;
            transform.position = Vector3.Lerp(startPoint, First_vibror , t) + new Vector3(-Offset, Offset, 0);

            if (timer >= duration_of_wave){
        
                timer = 0.0f;
                First_movement=false;
                StraightMovement=true;}
            }
       
            if(StraightMovement){
                timer += Time.deltaTime;
                
                float t = timer / (Time_factor*duration_of_wave); //calculate the speed of the movement from startPoint to finishPoint based on the total duration of the movement is seconds 

                transform.position = Vector3.Lerp(First_vibror , Last_vibror , t);

                if (timer >= (Time_factor*duration_of_wave)){
        
                timer = 0.0f;
                StraightMovement=false;
                Last_movement=true;}


                }

            if(Last_movement){
                timer += Time.deltaTime;
                
                float t = timer / duration_of_wave;
                float Offset = height_of_curve - height_of_curve *4*((t-0.5f)*(t-0.5f)) ;
                transform.position = Vector3.Lerp(Last_vibror , endPoint , t) + new Vector3(Offset, Offset, 0);

            //when the Trigger is in the correct place turned of the movement and reset the timer 
            if (timer >= duration_of_wave){
        
                timer = 0.0f;
                Last_movement=false;
                gameObject.SetActive(false);
                }
            }

        

            
        }

        
        
    

    public void StartMovement_Trigger(Vector3 st,Vector3 fv,Vector3 lv,Vector3 ed)
    {
        startPoint = st;
        First_vibror = fv;
        Last_vibror = lv;
        endPoint = ed;
        gameObject.SetActive(true);
        First_movement = true;
    }

    // the information about the speed of the Trigger movement is read from the .csv file during the experiment
    public void UpdateSpeed_Trigger(float visualSpeed)
    {
        speed = visualSpeed;
        duration_of_wave = 9f/(visualSpeed*Time_factor);

    }


}