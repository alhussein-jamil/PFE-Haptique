using System.Collections.Generic;
using Franka;
using UnityEngine;

public class GManager : MonoBehaviour
{
    private RedisConnection redisConnection;
    public Dictionary<string, string> gameParameters = new Dictionary<string, string>();
    private bool subscribed = false;
    
    public double[][] robotCalibrationData = null;    
    public int calibrationDataLength = 0;
    public string gameParametersString = "";
    public string mainSceneName = "RobotScene";
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        redisConnection = GetComponent<RedisConnection>();
        gameParameters["SceneType"] = "robot";
        gameParameters["Side"] = "right";
        //initialize game parameters
        gameParameters["stim.visuel"] = "pinceau";
        gameParameters["velocite.tactile"] = "3";
        gameParameters["velocite.visuel"] = "3";
        gameParameters["congruency"] = "congruent";
        gameParameters["pleasantness"] = "X";
        gameParameters["intensity"] = "X";
        //go to main scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainSceneName);
        
    }
    string DictToString(Dictionary<string, string> dict)
    {
        string s = "";
        foreach (KeyValuePair<string, string> entry in dict)
        {
            s += entry.Key + ";" + entry.Value + "\n";
        }
        return s;
    }
    public void setCalibrationData(double[][] data)
    {

        robotCalibrationData = data;
        Debug.Log("Calibration data set " + robotCalibrationData.Length);
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
        if(Input.GetKeyDown(KeyCode.V))
            // go to robot scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("RobotScene");
        if(Input.GetKeyDown(KeyCode.Q))
            // go to main scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        calibrationDataLength = robotCalibrationData == null ? 0 : robotCalibrationData.Length;
        gameParametersString = DictToString(gameParameters);
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
