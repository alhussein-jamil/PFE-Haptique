using System;
using System.Collections.Generic;
using StackExchange.Redis;
using UnityEngine;

namespace Franka
{
    public class Oscillation : MonoBehaviour
    {
        public float amplitude = 2.0f;
        public float frequency = 0.5f;
        public double[] encoderValues;
        private RedisConnection redisConnection;

        private ISubscriber publisher;

        void Start()
        {
            encoderValues = new double[7];

            redisConnection = GetComponent<RedisConnection>();
            publisher = redisConnection.publisher;
            InvokeRepeating("Publish", 0f, 0.001f);
        }

        void Publish()
        {
            if (!redisConnection.doneInit)
                return;

            for (int idx = 0; idx < encoderValues.Length; idx++)
            {
                encoderValues[idx] = amplitude * Mathf.Sin(frequency * Time.time);
            }
            byte[] bytes = RedisConnection.CoordsToLine(encoderValues).ToArray();

            publisher.Publish(redisConnection.simRobotChannel, bytes);
        }
    }
}
