using UnityEngine;
using TMPro;

namespace Franka{


public class onoff : MonoBehaviour
{
    public RedisConnection redisConnection;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (redisConnection.redis.IsConnected){
            // set the text to on or off
            this.GetComponent<TMP_Text>().text = "ON";
            // set the color to green
            this.GetComponent<TMP_Text>().color = Color.green;
        }
        else{
            // set the text to on or off
            this.GetComponent<TMP_Text>().text = "OFF";
            // set the color to red
            this.GetComponent<TMP_Text>().color = Color.red;
        }
        // if(Application.internetReachability == NetworkReachability.NotReachable)
        // {
        //     this.GetComponent<TMP_Text>().text = "Cannot connect to server";
        // }
        // else
        // {
        //     this.GetComponent<TMP_Text>().text = "Connected to server";
        //     // put the ip address of the server here
        //     this.GetComponent<TMP_Text>().text += "\n" + redisConnection.connection_string;
        // }
    }
}
}