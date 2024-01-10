using UnityEngine;
using StackExchange.Redis;

namespace Franka{
    public class RedisConnection : MonoBehaviour
    {
        public ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");//"192.168.154.84:6379");
        public IDatabase db; 
        public ISubscriber subscriber ;
        public ISubscriber publisher;
        public RedisChannel sim_robot_channel;
        public RedisChannel robot_channel;

        // Start is called before the first frame update
        void Start()
        {
            db = redis.GetDatabase();
            subscriber = redis.GetSubscriber();
            publisher = redis.GetSubscriber();
            robot_channel = new RedisChannel("Robot_Encoders", RedisChannel.PatternMode.Auto);
            sim_robot_channel = new RedisChannel("Sim_Robot_Encoders", RedisChannel.PatternMode.Auto);
        }

    }

}
