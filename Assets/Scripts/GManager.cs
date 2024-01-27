using System.Collections;
using System.Collections.Generic;
using Franka;
using UnityEngine;

public class GManager : MonoBehaviour
{
    private RedisConnection redisConnection;
    public Dictionary<string, string> gameParameters = new Dictionary<string, string>();
    private bool subscribed = false;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        redisConnection = GetComponent<RedisConnection>();

        //initialize game parameters
        gameParameters["stim.visuel"] = "pinceau";
        gameParameters["velocite.tactile"] = "3";
        gameParameters["velocite.visuel"] = "3";
        gameParameters["congruency"] = "congruent";
        gameParameters["pleasantness"] = "X";
        gameParameters["intensity"] = "X";
        
    }

    (string,  string) ParseGameParameters(string line)
    {
        string[] split = line.Split(';');
        string key = split[0];
        string value = split[1];
        return (key, value);
    }

    // Update is called once per frame
    void Update()
    {
        if (!redisConnection.doneInit)
            return;
        if (!subscribed)
        {
            redisConnection.subscriber.Subscribe(redisConnection.redisChannels["game_parameters"], (channel, message) =>
            {
                string line = message.ToString();
                (string key, string value) = ParseGameParameters(line);
                gameParameters[key] = value;
                Debug.Log("Received game parameter: " + key + " " + value);

            });
            subscribed = true;
        }
        
    }
}
