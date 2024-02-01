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
        public GameObject[] links;
        public bool calibrationDone = false;
        public bool subscriptionDone = false;
        public bool firstMove = false;
        public GameObject brush;

        public RobotType robotDropdown;
        public float speedScale = 1f;  
        private float updateFrequency = 1000f;
        private double stiffness = 100000;
        private double damping = 10000;

        private ArticulationBody[] articulationChain;
        private double[] encoderValues;
        private RedisConnection redisConnection;
        private Queue<double[]> valueBuffer = new Queue<double[]>();
        private const int bufferSize = 200;
        private RedisChannel robotChannel;
        private double[][] messages;
        private List<double[]> messageList;
        private bool realRobotMoving = false;
        private int idx;
        private double _speedScale = 1f;
        private bool simRobotMoving = false;
        private GameObject gameManager;
        private bool calibDataSet = false;
        private float calibrationSpeed = 0.5f;
        private bool reversed = false;

        private void Awake()
        {
            brush.SetActive(false);
            gameManager = GameObject.Find("GameManager");
            links = gameObject.GetComponentsInChildren<Transform>().Where(t => t.name.Contains("panda_link")).Select(t => t.gameObject).ToArray();
            // remove the first link (base)
            links = links.Skip(1).ToArray();
            redisConnection = gameManager.GetComponent<RedisConnection>();
            messageList = new List<double[]>();
            encoderValues = new double[links.Length];
            _speedScale = Math.Abs(speedScale);

            if (gameManager.GetComponent<GManager>().robotCalibrationData != null)
            {
                messages = gameManager.GetComponent<GManager>().robotCalibrationData;
                calibrationDone = true;
                idx = messages.Length;
            }

            InvokeRepeating("UpdateTargets", 0f, 1f / updateFrequency);
            articulationChain = links.Select(link => link.GetComponent<ArticulationBody>()).ToArray();

            foreach (var articulation in articulationChain)
            {
                articulation.SetDriveStiffness(ArticulationDriveAxis.X, (float)stiffness);
                articulation.SetDriveDamping(ArticulationDriveAxis.X, (float)damping);
            }
        }

        private void CalibrateRobot()
        {
            redisConnection.subscriber.UnsubscribeAll(flags: CommandFlags.FireAndForget);
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
            speedScale = 1f;
            messages = null;

            messageList.Clear();
            Awake();
        }

        private void SubscribeToRedis()
        {
            robotChannel = (robotDropdown == RobotType.SimRobot) ? redisConnection.redisChannels["sim_encoder_positions"] : redisConnection.redisChannels["encoder_positions"];
            var robotRedisSubscriber = redisConnection.subscriber.Subscribe(robotChannel);
            robotRedisSubscriber.OnMessage(message =>
            {
                if (calibrationDone)
                    return;

                double[] parsedMessage = RedisConnection.ParseMessage(message);
                if (valueBuffer.Count >= bufferSize)
                    valueBuffer.Dequeue();

                valueBuffer.Enqueue(parsedMessage);

                var lastState = realRobotMoving;
                realRobotMoving = CheckSignificantMovement();

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
            });

            var caresseSubscriber = redisConnection.subscriber.Subscribe(redisConnection.redisChannels["caresse"]);
            caresseSubscriber.OnMessage(message =>
            {
                if (!calibrationDone)
                {
                    calibrationSpeed = (float)Convert.ToDouble(message.Message);
                    Debug.Log("Received calibration speed: " + calibrationSpeed);
                }
                else
                {
                    idx = 0;
                    simRobotMoving = true;
                    _speedScale = Math.Abs(speedScale);
                }
            });
        }

        private void UpdateTargets()
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

            if ((int)(idx * _speedScale * 1000.0f / updateFrequency) < messages.Length && simRobotMoving)
            {
                int messageIdx = (int)(idx * _speedScale * 1000 / updateFrequency);
                double[] commandValues = reversed
                    ? (messages.Length - 1 - messageIdx >= 0 && messages.Length - 1 - messageIdx < messages.Length
                        ? messages[messages.Length - 1 - messageIdx]
                        : null)
                    : messages[messageIdx];

                if (commandValues is null)
                    return;

                for (int id = 0; id < commandValues.Length; id++)
                {
                    encoderValues[id] = commandValues[id] * 180 / Math.PI;
                }

                idx++;
            }
            else
            {
                _speedScale = Math.Abs(speedScale);
                reversed = speedScale < 0;
                simRobotMoving = false;
            }
        }

        private void Update()
        {
            if (simRobotMoving && calibrationDone)
                brush.SetActive(true);
            else
                brush.SetActive(false);

            speedScale = float.Parse(gameManager.GetComponent<GManager>().gameParameters["velocite.visuel"]) / calibrationSpeed;

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

        private void SetTargets(double[] targets)
        {
            for (int i = 0; i < links.Length; i++)
            {
                articulationChain[i].SetDriveTarget(ArticulationDriveAxis.X, (float)targets[i]);
            }
        }

        private bool CheckSignificantMovement()
        {
            if (valueBuffer.Count < bufferSize)
                return false;

            double[] first = valueBuffer.Peek();
            double[] last = valueBuffer.Last();

            double movementThreshold = 0.01;
            double totalDifference = first.Zip(last, (f, l) => Math.Abs(f - l)).Sum();
            return totalDifference > movementThreshold;
        }
    }
}
