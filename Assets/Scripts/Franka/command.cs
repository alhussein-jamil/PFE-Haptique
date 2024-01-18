using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using UnityEngine;
namespace Franka
{
    public class Command : MonoBehaviour
    {
        public enum RobotType { SimRobot, Robot };
        public ArticulationBody[] articulationChain;
        public double[] encoderValues;
        private RedisConnection redisConnection;

        // Dropdown menu for different channels
        public RobotType robotDropdown;
        public RedisChannel robotChannel;
        private Queue<ChannelMessage> messageQueue;

        public bool subscriptionDone = false;

        // Start is called before the first frame update
        void Start()
        {
            redisConnection = GetComponent<RedisConnection>();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            encoderValues = new double[articulationChain.Length];
            messageQueue = new Queue<ChannelMessage>();
            InvokeRepeating("updateTargets", 0f, 0.001f);

        }

        void SetTargets(double[] targets)
        {
            for (int idx = 0; idx < targets.Length; idx++)
            {
                if (idx + 1 < articulationChain.Length)
                    articulationChain[idx + 1].SetDriveTarget(axis: ArticulationDriveAxis.X, value: (float)targets[idx]);
            }
        }

        public void ParseMessage(ChannelMessage message)
        {
            // split the message into a list of bytes
            List<byte> bytes = System.Text.Encoding.Unicode.GetBytes(message.Message.ToString()).ToList();

            // Cut the message into an array of strings 
            double[] commandValues = LineToCoords(bytes).ToArray();
            for (int idx = 0; idx < commandValues.Length - 1; idx++)
            {
                encoderValues[idx] = commandValues[idx] * 180 / Math.PI;
            }
        }


        void SubscribeToRedis()
        {
            robotChannel = (robotDropdown == RobotType.SimRobot) ? redisConnection.simRobotChannel : redisConnection.robotChannel;
            var channel = redisConnection.subscriber.Subscribe(robotChannel);
            channel.OnMessage(message =>
            {
                messageQueue.Enqueue(message);

            }
            );
        }


        void updateTargets()
        {
            if (messageQueue.Count > 0)
            {
                ParseMessage(messageQueue.Dequeue());
            }
            else
            {
                Debug.Log("No messages in queue");
            }
        }


        static List<double> LineToCoords(List<byte> bytes)
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

        // Update is called once per frame
        void Update()
        {
            if (!subscriptionDone && redisConnection.doneInit)
            {
                SubscribeToRedis();
                subscriptionDone = true;
            }
            SetTargets(encoderValues);
        }
    }
}
