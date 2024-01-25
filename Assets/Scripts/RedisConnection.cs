using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using Unity.VisualScripting;
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
        public RedisChannel simRobotChannel;
        public RedisChannel robotChannel;
        public RedisChannel caresseChannel;
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
                redis = ConnectionMultiplexer.Connect(connection_string + " ,abortConnect=false");
                db = redis.GetDatabase();
                subscriber = redis.GetSubscriber();
                publisher = redis.GetSubscriber();
                RedisChannel testChannel = new RedisChannel("test", RedisChannel.PatternMode.Auto);
                //publish random message
                publisher.Publish(testChannel, test_message);
                //subscribe to test channel
                subscriber.Subscribe(
                    testChannel,
                    (channel, message) =>
                    {
                        Debug.Log((string)message);
                    }
                );

                robotChannel = new RedisChannel("encoder_positions", RedisChannel.PatternMode.Auto);
                simRobotChannel = new RedisChannel("Sim_Robot_Encoders", RedisChannel.PatternMode.Auto);
                caresseChannel = new RedisChannel("caresse", RedisChannel.PatternMode.Auto);
            }

            doneInit = true;

        }
    }
}
