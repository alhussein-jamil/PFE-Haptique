using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;

namespace Franka{

public class MovementManager : MonoBehaviour
{

    public GameObject GameManager;

private GManager gmanager;
    public Dictionary<string, string> gameParameters = new Dictionary<string, string>();

    private bool setSource = false;
    private RedisConnection redisConnection;

    private bool subscriptionDone = false;

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
        if (GameManager == null)
                GameManager = GameObject.Find("GameManager");

        gmanager = GameManager.GetComponent<GManager>();

        gameParameters = gmanager.gameParameters;

        redisConnection = GameManager.GetComponent<RedisConnection>();

        foreach (KeyValuePair<string, string> paire in gameParameters)
        {
            string cle = paire.Key;
            string valeur = paire.Value;

            // Affichez la clé et la valeur dans la console Unity
            Debug.Log("Clé: " + cle + ", Valeur: " + valeur);
        }

    }


    void SubscribeToRedis()
        {
            var virtualCaresseSubscriber = redisConnection.subscriber.Subscribe(redisConnection.redisChannels["caresse"]);
            virtualCaresseSubscriber.OnMessage(message =>
            {
                buttonclicked();
                Debug.Log("channel reçu");
            }
            );
    }


    public void buttonclicked()
        {
            setSource = true;

        }

    //
    void NextMouvement()
    {
 
        if(gameParameters["congruency"].Contains("mismatch")){
            mismatch = true;
        }

        else{mismatch = false;}

        manager_gen.SetSourceBySpeed( (gameParameters["velocite.tactile"]).ToString() , mismatch);

        startPos = startPoint.position;
        First_vibror_Pos = First_vibror.position;
        Last_vibror_Pos = Last_vibror.position;
        endPos = endPoint.position;

       

       if(gameParameters["stim.visuel"] == "pinceau" ){
            Debug.Log("lapince");
            brush_script.UpdateSpeed_Brush(float.Parse(gameParameters["velocite.visuel"], CultureInfo.InvariantCulture));
            brush_script.StartMovement_Brush(startPos,First_vibror_Pos,Last_vibror_Pos,endPos);
       }


       if(gameParameters["stim.visuel"]  == "fleche" ){
            arrow_script.UpdateSpeed_Arrow(float.Parse(gameParameters["velocite.visuel"], CultureInfo.InvariantCulture));
            arrow_script.StartMovement_Arrow(startPos,First_vibror_Pos,Last_vibror_Pos,endPos);
       }

       if(gameParameters["congruency"]  == "mismatch_incongruent " || gameParameters["congruency"]  == "mismatch_congruent"){
            startPos = endPoint.position;
            First_vibror_Pos = Last_vibror.position;
            Last_vibror_Pos = First_vibror.position;
            endPos = startPoint.position;
       }

        
        Debug.Log("test_trigg");

        trigger_script.UpdateSpeed_Trigger(float.Parse(gameParameters["velocite.tactile"], CultureInfo.InvariantCulture));
        trigger_script.StartMovement_Trigger(startPos,First_vibror_Pos,Last_vibror_Pos,endPos);
        


    }



void Update()
{
    if (!subscriptionDone   )
    {
        SubscribeToRedis();
        subscriptionDone = true;
    }
    if(setSource)
    {
        NextMouvement();
        setSource = false;

}
}


}

}


