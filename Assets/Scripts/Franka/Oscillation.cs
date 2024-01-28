using System;
using UnityEngine;

namespace Franka
{
    public class Oscillation : MonoBehaviour
    {
        public double amplitude = 2.0f;
        public double frequency = 0.5f;
        public double[] encoderValues;
        private RedisConnection redisConnection;

        public bool Moving = false;
        public double pubInterval = 0.001f;
        public double lastValue = 0.0f;

        void Start()
        {
            encoderValues = new double[7];

            redisConnection = GetComponent<RedisConnection>();

            InvokeRepeating("Publish", 0f, (float)pubInterval);
        }

        void Publish()
        {

            if (!redisConnection.doneInit)
                return;

            for (int idx = 0; idx < encoderValues.Length; idx++)
            {
                if(Moving)
                {
                    lastValue += frequency * pubInterval;
                encoderValues[idx] = amplitude * Math.Sin(lastValue);
                }
            
            }
            byte[] bytes = RedisConnection.CoordsToLine(encoderValues).ToArray();

            redisConnection.publisher.Publish(redisConnection.redisChannels["sim_encoder_positions"], bytes);
        }
    }
}
