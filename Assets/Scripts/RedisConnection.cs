using UnityEngine;
using StackExchange.Redis;

namespace Franka{
    public class RedisConnection : MonoBehaviour
    {
        public ConnectionMultiplexer redis;
        
        public string connection_string = "localhost:6379";
        public IDatabase db; 
        public ISubscriber subscriber ;
        public ISubscriber publisher;
        public RedisChannel simRobotChannel;
        public RedisChannel robotChannel;
        public bool doneInit = false;

        // Start is called before the first frame update
        void Start()
        {
            redis = ConnectionMultiplexer.Connect(connection_string +" ,abortConnect=false");
            db = redis.GetDatabase();
            subscriber = redis.GetSubscriber();
            publisher = redis.GetSubscriber();
            robotChannel = new RedisChannel("Robot_Encoders", RedisChannel.PatternMode.Auto);
            simRobotChannel = new RedisChannel("Sim_Robot_Encoders", RedisChannel.PatternMode.Auto);
            doneInit = true;
        }

    }

}
