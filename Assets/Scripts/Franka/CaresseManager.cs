using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Franka
{
    public class CaresseManager : MonoBehaviour
    {
        private RedisConnection redisConnection;
        public double caresseSpeedRobot;
        public string usedObject;
        public double caresseSpeedBrush;
        public string congruency;
        public bool Subscribe = true;
        public int speedidx = 0;
        public string csvPath = "Assets/Data/Data_participant_Incongruency.csv";

        public string[] objects;
        public double[] caresseSpeeds;
        public double[] brushSpeeds;
        public string[] congruencies;

        void Start()
        {
            redisConnection = GetComponent<RedisConnection>();
        
            (objects, caresseSpeeds, brushSpeeds, congruencies) = getValues(csvPath);
            setValues();
        }

    private (string[], double[], double[], string[])  getValues(string csvPath)
        {
            List<string> firstColumn = new List<string>();
            List<double> secondColumn = new List<double>();
            List<double> thirdColumn = new List<double>();
            List<string> fourthColumn = new List<string>();
            var firstLine = true;
            IFormatProvider iFormatProvider = new System.Globalization.CultureInfo("en-US");

            try
            {
                using (var reader = new StreamReader(csvPath))
                {
                    Debug.Log("Reading csv file");
                    while (reader.Peek() >= 0)
                    {
                        var line = reader.ReadLine();
                        if (line == ";;;;;")
                            break;

                        if (firstLine)
                        {
                            firstLine = false;
                            continue;
                        }
                        var values = line.Split(';');
                        firstColumn.Add(values[0]);
                        //make sure to have the correct format
                        secondColumn.Add(Convert.ToDouble(values[1], iFormatProvider));
                        thirdColumn.Add(Convert.ToDouble(values[2], iFormatProvider));
                        fourthColumn.Add(values[3]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
            return (firstColumn.ToArray(), secondColumn.ToArray(), thirdColumn.ToArray(), fourthColumn.ToArray());
        }

        public void publishCaresse()
        {
            if (!redisConnection.doneInit)
                return;
            redisConnection.publisher.Publish(redisConnection.caresseChannel, caresseSpeedRobot.ToString());
        }

        public void setValues()
        {
            if (!redisConnection.doneInit)
                return;
            usedObject = objects[speedidx];
            caresseSpeedRobot = caresseSpeeds[speedidx];
            caresseSpeedBrush = brushSpeeds[speedidx];
            congruency = congruencies[speedidx];

        }

        public void nextSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            if (speedidx == objects.Length - 1)
                speedidx = 0;
            else
                speedidx++;
        }

        public void previousSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            if (speedidx == 0)
                speedidx = objects.Length - 1;
            else
                speedidx--;
        }

        void Update()
        {
            if (redisConnection.doneInit && Subscribe)
            {
                var channel = redisConnection.subscriber.Subscribe(redisConnection.caresseChannel);

                //subscribe to the channel
                channel.OnMessage(message =>
                {
                    double parsedValue = double.Parse(message.Message);
                    Debug.Log("Received " + parsedValue + " from " + channel);
                });
                Subscribe = false;
            }
            setValues();
        }
    }
}
