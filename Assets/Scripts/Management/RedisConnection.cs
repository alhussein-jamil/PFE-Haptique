using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using UnityEngine;

namespace Franka
{
    public class RedisConnection : MonoBehaviour
    {
        public ConnectionMultiplexer redis;

        public string connection_string = "localhost:6379";
        public string test_message = "Hello World";
        public IDatabase db;
        public ISubscriber subscriber;
        public ISubscriber publisher;
        public string[] channels = new string[] { "encoder_positions", "sim_encoder_positions", "robot_caresse" };
        public Dictionary<string, RedisChannel> redisChannels = new Dictionary<string, RedisChannel>();

        public bool doneInit = false;
        public bool requiresRedis = false;
        public static List<double> LineToCoords(List<byte> bytes)
        {
            List<double> ret = new List<double>();

            for (int i = 0; i < bytes.Count; i += 8)
            {
                byte[] b = bytes.Skip(i).Take(8).ToArray();
                // make sure you have 8 bytes
                if (b.Length < 8)
                    break;
                ret.Add(BitConverter.ToDouble(b, 0));
            }

            return ret;
        }

        public static List<byte> CoordsToLine(double[] coords)
        {
            List<byte> ret = new List<byte>();

            foreach (var coord in coords)
            {
                byte[] bytes = BitConverter.GetBytes(coord);
                ret.AddRange(bytes);
            }

            return ret;
        }

        public static double[] ParseMessage(ChannelMessage message)
        {
            // split the message into a list of bytes
            List<byte> bytes = ((byte[])message.Message).ToList();

            // Cut the message into an array of strings
            double[] parsedValues = LineToCoords(bytes).ToArray();

            return parsedValues;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (requiresRedis)
            {
                redis = ConnectionMultiplexer.Connect(connection_string);
                db = redis.GetDatabase();
                subscriber = redis.GetSubscriber();
                publisher = redis.GetSubscriber();
                foreach (var channel in channels)
                {
                    redisChannels.Add(channel, new RedisChannel(channel, RedisChannel.PatternMode.Auto));
                }

            }

            doneInit = true;

        }
    }
}
