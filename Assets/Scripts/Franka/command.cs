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
        public bool calibrationDone = false;
        private RedisConnection redisConnection;

        // Dropdown menu for different channels
        public RobotType robotDropdown;
        private RedisChannel robotChannel;
        // private Queue<ChannelMessage> messageQueue;

        public bool subscriptionDone = false;
        private double[][] messages;
        private List<double[]> messageList;
        private byte[] lastMessage = new byte[0];
        public bool firstMove = false;
        private bool realRobotMoving = false;
        public float speedScale = 1f;
        private int idx;
        private double _speedScale = 1f;
        private bool simRobotMoving = false;
        // Start is called before the first frame update
        public float updateFrequency = 1000f;
        void Start()
        {
            messageList = new List<double[]>();
            redisConnection = GetComponent<RedisConnection>();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            encoderValues = new double[articulationChain.Length];
            _speedScale = speedScale;
            InvokeRepeating("updateTargets", 0f, 1f / updateFrequency);

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

                // calculate the norm2 difference of last message and current message ie messageList.Last() and messageList.Last( - 1)
                var lastState = realRobotMoving;

                realRobotMoving = !lastMessage.SequenceEqual(new byte[0]) && !lastMessage.SequenceEqual((byte[])message.Message);
                if (!realRobotMoving)
                    _speedScale = speedScale;

                if (!lastState && realRobotMoving)
                {
                    idx = 0;
                    simRobotMoving = true;
                }

                if (!firstMove)
                    firstMove = realRobotMoving;

                if (realRobotMoving && !calibrationDone)
                    messageList.Add(RedisConnection.ParseMessage(message));
                if (!calibrationDone && firstMove && !realRobotMoving)
                {
                    messages = messageList.ToArray();
                    messageList.Clear();
                    calibrationDone = true;
                    idx = messages.Length;

                }


                lastMessage = (byte[])message.Message;

            }
            );
        }


        void updateTargets()
        {
            if (!calibrationDone)
            {
                simRobotMoving = false;
                return;
            }
            if ((int)(idx * _speedScale) < messages.Length && simRobotMoving)
            {

                double[] commandValues = messages[(int)(idx * _speedScale)];
                for (int idx = 0; idx < commandValues.Length; idx++)
                {
                    encoderValues[idx] = commandValues[idx] * 180 / Math.PI;
                }
                idx++;
            }
            else
            {
                _speedScale = speedScale;
                simRobotMoving = false;
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
