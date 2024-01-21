using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Franka
{
    public class CaresseManager : MonoBehaviour
    {
        private RedisConnection redisConnection;
        public double caresseSpeed;
        public bool Subscribe = true;
        public double[] valuesToSend;
        public int speedidx = 0;
        public string csvPath = "Assets/Data/caresse.csv";

        void Start()
        {
            redisConnection = GetComponent<RedisConnection>();
            valuesToSend = getValues(csvPath);
            if (valuesToSend.Length > 0)
                caresseSpeed = valuesToSend[0];
        }

        private double[] getValues(string csvPath)
        {
            List<double> secondColumn = new List<double>();
            try
            {
                using (var reader = new StreamReader(csvPath))
                {
                    Debug.Log("Reading csv file");
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(';');
                        secondColumn.Add(Convert.ToDouble(values[1]));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
            return secondColumn.ToArray();
        }

        public void publishCaresse()
        {
            if (!redisConnection.doneInit)
                return;
            byte[] bytes = RedisConnection.CoordsToLine(new double[] { caresseSpeed }).ToArray();
            string message = System.Text.Encoding.Unicode.GetString(bytes);
            //Publish the message
            redisConnection.publisher.Publish(redisConnection.caresseChannel, message);
        }

        public void setSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            caresseSpeed = valuesToSend[speedidx];
        }

        public void nextSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            if (speedidx == valuesToSend.Length - 1)
                speedidx = 0;
            else
                speedidx++;
        }

        public void previousSpeed()
        {
            if (!redisConnection.doneInit)
                return;
            if (speedidx == 0)
                speedidx = valuesToSend.Length - 1;
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
                    double[] parsedValues = RedisConnection.ParseMessage(message);
                    Debug.Log("Received " + parsedValues[0].ToString() + " from " + channel);
                });
                Subscribe = false;
            }
        }
    }
}
