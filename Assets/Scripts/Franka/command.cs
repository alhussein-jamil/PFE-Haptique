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
        private ArticulationBody[] articulationChain;
        public double[] encoderValues;
        private RedisConnection redisConnection;

        // Dropdown menu for different channels
        public RobotType robotDropdown;
        private RedisChannel robotChannel;

        public bool calibrationDone = false;
        private double[][] messages;
        private List<double[]> messageList;
        public bool subscriptionDone = false;
        private byte[] lastMessage = new byte[0];
        public bool firstMove = false;
        private bool realRobotMoving = false;
        private float speedScale = 1f;
        private int idx;
        private double _speedScale = 1f;
        private bool simRobotMoving = false;
        public GameObject[] links;
        // Start is called before the first frame update
        public float updateFrequency = 1000f;
        public double stiffness = 100000;
        public double damping = 10000;
        private bool reversed = false;
        public GameObject gameManager;
        private bool calibDataSet = false;
        public void CalibrateRobot()
        {
            
            //unsubscribe from redis
            redisConnection.subscriber.UnsubscribeAll();
            Debug.Log("Resetting robot");
            CancelInvoke();
            gameManager.GetComponent<GManager>().robotCalibrationData = null;
            calibrationDone = false;
            subscriptionDone = false;
            firstMove = false;
            realRobotMoving = false;
            simRobotMoving = false;
            calibDataSet = false;
            idx = 0;
            _speedScale = 1f;
            lastMessage = new byte[0];
            messages = null;
            messageList.Clear();
            Start();
        }
        void Start()
        {
            if (gameManager == null)
                gameManager = GameObject.Find("GameManager");

            redisConnection = gameManager.GetComponent<RedisConnection>();

            messageList = new List<double[]>();
            encoderValues = new double[links.Length];
            _speedScale = Math.Abs(speedScale);
            // check for calibration data existence in the game manager
            if (gameManager.GetComponent<GManager>().robotCalibrationData != null)
            {
                messages = gameManager.GetComponent<GManager>().robotCalibrationData;
                calibrationDone = true;
                idx = messages.Length;
            }
            InvokeRepeating("updateTargets", 0f, 1f / updateFrequency);
            articulationChain = new ArticulationBody[links.Length];
            for (int idx = 0; idx < links.Length; idx++)
            {
                articulationChain[idx] = links[idx].GetComponent<ArticulationBody>();
                articulationChain[idx].SetDriveStiffness(axis: ArticulationDriveAxis.X, value: (float)stiffness);
                articulationChain[idx].SetDriveDamping(axis: ArticulationDriveAxis.X, value: (float)damping);
            }
        }

        void SetTargets(double[] targets)
        {
            for (int idx = 0; idx < links.Length; idx++)
            {
                articulationChain[idx].SetDriveTarget(axis: ArticulationDriveAxis.X, value: (float)targets[idx]);
            }
        }


        void SubscribeToRedis()
        {
            robotChannel = (robotDropdown == RobotType.SimRobot) ? redisConnection.redisChannels["sim_encoder_positions"] : redisConnection.redisChannels["encoder_positions"];
            var robotRedisSubscriber = redisConnection.subscriber.Subscribe(robotChannel);
            robotRedisSubscriber.OnMessage(message =>
            {
                // calculate the norm2 difference of last message and current message ie messageList.Last() and messageList.Last( - 1)
                var lastState = realRobotMoving;

                realRobotMoving = !lastMessage.SequenceEqual(new byte[0]) && !lastMessage.SequenceEqual((byte[])message.Message);
                if (!realRobotMoving)
                    _speedScale = Math.Abs(speedScale);

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
            var virtualCaresseSubscriber = redisConnection.subscriber.Subscribe(redisConnection.redisChannels["virtual_caresse"]);
            virtualCaresseSubscriber.OnMessage(message =>
            {
                speedScale = (float)Convert.ToDouble(message.Message);
                Debug.Log("Speed scale: " + speedScale);
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
            else
            {
                if (!calibDataSet)
                {
                    gameManager.GetComponent<GManager>().setCalibrationData(messages);
                    calibDataSet = true;
                }
            }
            if ((int)(idx * _speedScale * 1000 / updateFrequency) < messages.Length && simRobotMoving)
            {
                int messageIdx = (int)(idx * _speedScale * 1000 / updateFrequency);

                double[] commandValues = null;

                if (reversed)
                {
                    if (messages.Length - 1 - messageIdx >= 0 && messages.Length - 1 - messageIdx < messages.Length)
                        commandValues = messages[messages.Length - 1 - messageIdx];
                }
                else
                    commandValues = messages[messageIdx];

                if (commandValues is null)
                    return;

                for (int idx = 0; idx < commandValues.Length; idx++)
                {
                    encoderValues[idx] = commandValues[idx] * 180 / Math.PI;
                }
                idx++;
            }
            else
            {
                _speedScale = Math.Abs(speedScale);
                if (speedScale < 0)
                    reversed = true;
                else
                    reversed = false;
                simRobotMoving = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                CalibrateRobot();
            if (redisConnection.redis.IsConnected)
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
