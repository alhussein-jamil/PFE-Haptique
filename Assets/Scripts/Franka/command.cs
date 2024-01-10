using NetTopologySuite.Operation.Buffer;
using StackExchange.Redis;
using Unity.Robotics.UrdfImporter;
using UnityEngine;
using UnityEngine.UI;


namespace Franka{
public class command : MonoBehaviour
{
    public enum RobotType {SimRobot, Robot};
    public ArticulationBody[] articulationChain;
    public float[] encoderValues;
    private RedisConnection redisConnection;

    // make a dropdown menu for the different channels
    public RobotType robot_dropdown;

    public RedisChannel robot_channel;  

    public bool subsciption_done = false;

    // make a dropdown menu for the different robots

    // Start is called before the first frame update
    void Start()
    {

        redisConnection = this.GetComponent<RedisConnection>();

        articulationChain = this.GetComponentsInChildren<ArticulationBody>();

        encoderValues = new float[articulationChain.Length];

    }

    void SetTargets(float[] targets)
    {
        for(int idx = 0; idx < targets.Length; idx++)
        {
            if (idx +1 < articulationChain.Length)
                articulationChain[idx+1].SetDriveTarget(axis: ArticulationDriveAxis.X, value: targets[idx]);
        }
    }

    void SubscribeToRedis()
    {

        if (robot_dropdown == RobotType.SimRobot)
            robot_channel = redisConnection.sim_robot_channel;
        else
            robot_channel = redisConnection.robot_channel;
        redisConnection.subscriber.Subscribe(robot_channel, (channel, message) =>
        {
            // cut the message into an array of strings 
            string[] message_array = message.ToString().Split(';'); 
            for(int idx = 0; idx < message_array.Length-1; idx++)
            {
                encoderValues[idx] = float.Parse(message_array[idx]);
            }
        }
    );
    }
    // Update is called once per frame
    void Update()
    {
        SetTargets(encoderValues);

        if (!subsciption_done && redisConnection.doneInit)
        {
            SubscribeToRedis();
            subsciption_done = true;
        }
    }
}

}
