using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

class CsvParser
{
    public static List<Dictionary<string, string>> ReadCsvToDictionaryList(string csvPath)
    {

        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

        // Check if the file exists
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"File not found: {csvPath}");
        }

        // Read all lines from the CSV file
        string[] lines = File.ReadAllLines(csvPath);

        // Check if the file has at least one line
        if (lines.Length == 0)
        {
            throw new InvalidOperationException("CSV file is empty.");
        }
        // Get the header row (first row)
        string[] headers = lines[0].Split(';');

        // Iterate through the remaining rows and create dictionaries
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i] == ";;;;;")
                break;
            string[] values = lines[i].Split(';');

            // Create a dictionary for the current row
            Dictionary<string, string> rowDict = new Dictionary<string, string>();

            // Populate the dictionary with key-value pairs
            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                rowDict[headers[j]] = values[j];
            }

            // Add the dictionary to the result list
            result.Add(rowDict);
        }

        return result;
    }
}

namespace Franka
{

    public class CaresseManager : MonoBehaviour
    {
        private RedisConnection redisConnection;
        public GameObject gameManager;
        public bool Subscribe = true;
        public int speedidx = 0;
        public string csvPath = "Assets/Resources/Data_participant_Incongruency.csv";
        public List<Dictionary<string, string>> parsed;
        void Start()
        {

            gameManager = GameObject.Find("GameManager");
            redisConnection = gameManager.GetComponent<RedisConnection>();
            parsed = CsvParser.ReadCsvToDictionaryList(csvPath);

        }


        public void publishCaresse()
        {
            if (!redisConnection.doneInit)
                return;
            redisConnection.publisher.Publish(redisConnection.redisChannels["caresse"], gameManager.GetComponent<GManager>().gameParameters["velocite.tactile"]);
            float scale = float.Parse(gameManager.GetComponent<GManager>().gameParameters["velocite.visuel"]) / float.Parse(gameManager.GetComponent<GManager>().gameParameters["velocite.tactile"]);
            redisConnection.publisher.Publish(redisConnection.redisChannels["virtual_caresse"], scale.ToString());
        }

        public void setValues()
        {
            foreach (KeyValuePair<string, string> kvp in parsed[speedidx])
            {
                string message = kvp.Key + ";" + kvp.Value;
                Debug.Log("Sending " + message);
                redisConnection.publisher.Publish(redisConnection.redisChannels["game_parameters"], message);
            }

        }

        public void nextSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            speedidx = (speedidx + 1) % parsed.Count;
            setValues();
        }

        public void previousSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            speedidx = (speedidx - 1) % parsed.Count;
            setValues();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                nextSpeed();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                previousSpeed();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                publishCaresse();
            }
            if (redisConnection.doneInit && Subscribe)
            {
                var channel = redisConnection.subscriber.Subscribe(redisConnection.redisChannels["caresse"]);

                //subscribe to the channel
                channel.OnMessage(message =>
                {
                    double parsedValue = double.Parse(message.Message);
                    Debug.Log("Received " + parsedValue + " from " + channel);
                });
                Subscribe = false;
            }
        }
    }
}
