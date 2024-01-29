using StackExchange.Redis;

public class UDPRedisClient 
{
    public ConnectionMultiplexer redis;
    public string connection_string = "localhost:6379";
    public ISubscriber subscriber;
    string channel = "haptic_udp";
    UDPManagerRedis uDPManagerRedis;


    public bool subscribed = false;

    public UDPRedisClient(UDPManagerRedis uDPManagerRedis)
    {
        this.uDPManagerRedis = uDPManagerRedis;
    }

    // Start is called before the first frame update
    public void Start()
    {
        redis = ConnectionMultiplexer.Connect(connection_string);
        subscriber = redis.GetSubscriber();
        // call update at 60Hz
        float updateRate = 1.0f / 60.0f;
        float Timer = 0.0f;
        while (true)
        {
            Timer += DateTime.Now.Millisecond / 1000.0f;
            if (Timer > updateRate)
            {
                Timer -= updateRate;
                Update();
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        if(!subscribed && redis.IsConnected)
        {
            var x = subscriber.Subscribe(new RedisChannel("haptic_udp", RedisChannel.PatternMode.Auto));

            x.OnMessage(message => {
                
                string msg = message.Message.ToString();

                if(msg == "start")
                {   
                    uDPManagerRedis.StartCom();
                    System.Console.WriteLine("start UDP");
                }
                else if(msg == "bind")
                {
                    uDPManagerRedis.dataReceived += uDPManagerRedis.OnUDPMarginQueueReceived;
                    System.Console.WriteLine("bind UDP");
                }
                else
                {
                    uDPManagerRedis.SendData((byte[])message.Message);
                }
            });
            

            subscribed = true;
        }
        
    }

}
