using UnityEngine;
using StackExchange.Redis;

namespace Franka{
    public class RedisConnection : MonoBehaviour
    {
        public ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");//"192.168.154.84:6379");
        public IDatabase db; 
        public ISubscriber subscriber ;
        public ISubscriber publisher;
        public RedisChannel simRobotChannel;
        public RedisChannel robotChannel;
        public bool doneInit = false;

        // Start is called before the first frame update
        void Start()
        {
            db = redis.GetDatabase();
            subscriber = redis.GetSubscriber();
            publisher = redis.GetSubscriber();
            robotChannel = new RedisChannel("Robot_Encoders", RedisChannel.PatternMode.Auto);
            simRobotChannel = new RedisChannel("Sim_Robot_Encoders", RedisChannel.PatternMode.Auto);
            doneInit = true;
        }

    }

}
