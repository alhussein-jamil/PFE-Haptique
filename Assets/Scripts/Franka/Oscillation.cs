using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;
using System;

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


        static List<byte> CoordsToLine(double[] coords)
        {
            List<byte> ret = new List<byte>();

            foreach (var coord in coords)
            {
                byte[] bytes = BitConverter.GetBytes(coord);
                ret.AddRange(bytes);
            }

            return ret;
        }
        void Publish()
        {
            if (!redisConnection.doneInit)
                return;

            for (int idx = 0; idx < encoderValues.Length; idx++)
            {

                encoderValues[idx] = amplitude * Mathf.Sin(frequency * Time.time);

            }
            byte[] bytes = CoordsToLine(encoderValues).ToArray();
            string message = System.Text.Encoding.Unicode.GetString(bytes);
            publisher.Publish(redisConnection.simRobotChannel, message);
        }


    }
}