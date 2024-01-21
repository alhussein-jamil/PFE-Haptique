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
                double[] commandValues = RedisConnection.ParseMessage(messageQueue.Dequeue());
                for (int idx = 0; idx < commandValues.Length; idx++)
                {
                    encoderValues[idx] = commandValues[idx] * 180 / Math.PI;
                }
            }
            else
            {
                Debug.Log("No messages in queue");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (redisConnection.doneInit)
            {
                if (!subscriptionDone)
                {
                    SubscribeToRedis();
                    subscriptionDone = true;
                }

                    SetTargets(encoderValues);
            }
        }
    }
}
