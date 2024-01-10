using StackExchange.Redis;
using UnityEngine;

namespace Franka
{
    public class Command : MonoBehaviour
    {
        public enum RobotType { SimRobot, Robot };
        public ArticulationBody[] articulationChain;
        public float[] encoderValues;
        private RedisConnection redisConnection;

        // Dropdown menu for different channels
        public RobotType robotDropdown;
        public RedisChannel robotChannel;

        public bool subscriptionDone = false;

        // Start is called before the first frame update
        void Start()
        {
            redisConnection = GetComponent<RedisConnection>();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            encoderValues = new float[articulationChain.Length];
        }

        void SetTargets(float[] targets)
        {
            for (int idx = 0; idx < targets.Length; idx++)
            {
                if (idx + 1 < articulationChain.Length)
                    articulationChain[idx + 1].SetDriveTarget(axis: ArticulationDriveAxis.X, value: targets[idx]);
            }
        }

        void SubscribeToRedis()
        {
            robotChannel = (robotDropdown == RobotType.SimRobot) ? redisConnection.simRobotChannel : redisConnection.robotChannel;

            redisConnection.subscriber.Subscribe(robotChannel, (channel, message) =>
            {
                // Cut the message into an array of strings 
                string[] messageArray = message.ToString().Split(';');
                for (int idx = 0; idx < messageArray.Length - 1; idx++)
                {
                    encoderValues[idx] = float.Parse(messageArray[idx]);
                }
            });
        }

        // Update is called once per frame
        void Update()
        {
            SetTargets(encoderValues);

            if (!subscriptionDone && redisConnection.doneInit)
            {
                SubscribeToRedis();
                subscriptionDone = true;
            }
        }
    }
}
