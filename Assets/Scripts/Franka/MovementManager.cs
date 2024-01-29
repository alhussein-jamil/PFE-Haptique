using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Franka{

public class MovementManager : MonoBehaviour
{
    
    CaresseManager Instructs;

    public Transform startPoint; 
    public Transform First_vibror; 
    public Transform Last_vibror; 
    public Transform endPoint;

    private Vector3 startPos; 
    private Vector3 First_vibror_Pos; 
    private Vector3 Last_vibror_Pos; 
    private Vector3 endPos;

    public Brush_Movement brush_script;
    public Arrow_Movement arrow_script;
    public Trigger_Movement trigger_script;

    public Manager_Gen manager_gen;
    bool mismatch;


    // Start is called before the first frame update
    void Start()
    {
        Instructs = gameObject.GetComponent<CaresseManager>();
    }


    public void buttonclicked()
        {


            Invoke("NextMouvement", 1);
        }

    //
    void NextMouvement()
    {

        if(Instructs.getValue("congruency").Contains("mismatch")){
            mismatch = true;
        }
        else{mismatch = false;}

        manager_gen.SetSourceBySpeed( (Instructs.getValue("velocite.tactile")).ToString() , mismatch);

        startPos = startPoint.position;
        First_vibror_Pos = First_vibror.position;
        Last_vibror_Pos = Last_vibror.position;
        endPos = endPoint.position;

       if(Instructs.getValue("stim.visuel") == "pinceau" ){
            brush_script.UpdateSpeed_Brush(float.Parse(Instructs.getValue("velocite.visuel")));
            brush_script.StartMovement_Brush(startPos,First_vibror_Pos,Last_vibror_Pos,endPos);
       }


       if(Instructs.getValue("stim.visuel")  == "fleche" ){
            arrow_script.UpdateSpeed_Arrow(float.Parse(Instructs.getValue("velocite.visuel")));
            arrow_script.StartMovement_Arrow(startPos,First_vibror_Pos,Last_vibror_Pos,endPos);
       }

       if(Instructs.getValue("congruency")  == "mismatch_incongruent " || Instructs.getValue("congruency")  == "mismatch_congruent"){
            startPos = endPoint.position;
            First_vibror_Pos = Last_vibror.position;
            Last_vibror_Pos = First_vibror.position;
            endPos = startPoint.position;
       }

        
        trigger_script.UpdateSpeed_Trigger(float.Parse(Instructs.getValue("velocite.tactile")));
        trigger_script.StartMovement_Trigger(startPos,First_vibror_Pos,Last_vibror_Pos,endPos);
        


    }

}
}

