using StackExchange.Redis;
using System;
using System.Threading;
public class UDPRedisServer
{
    public ConnectionMultiplexer redis;
    public string connection_string = "localhost:6379";
    public ISubscriber subscriber;
    public ISubscriber publisher;   
    string channel = "haptic_udp";
    public int limit = 10;
    UDPManagerRedis uDPManagerRedis;


    public bool subscribed = false;

    public UDPRedisServer(UDPManagerRedis uDPManagerRedis, string ip_adress)
    {
        this.connection_string = ip_adress + ":6379";
        this.uDPManagerRedis = uDPManagerRedis;
    }

    // Start is called before the first frame update
    public void Start()
    {
        redis = ConnectionMultiplexer.Connect(connection_string);
        subscriber = redis.GetSubscriber();
        publisher = redis.GetSubscriber();
        // call update at 60Hz
        float updateRate = 1.0f / 60.0f;
        Timer update_timer = new Timer((e) => Update(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1.0f / 60.0f * 1000));
  }
    Queue<ChannelMessage > udpMarginQueue = new Queue<ChannelMessage >();



    void sendUDPCommands(){
                ChannelMessage  message = udpMarginQueue.Dequeue();
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
    }

    // Update is called once per frame
    void Update()
    {
        if(!subscribed && redis.IsConnected)
        {
            var x = subscriber.Subscribe(new RedisChannel(channel, RedisChannel.PatternMode.Auto));

            x.OnMessage(message => {
                udpMarginQueue.Enqueue(message);
                if (udpMarginQueue.Count > limit)
                {
                    udpMarginQueue.Dequeue();
                }
            });
            

            subscribed = true;
        }
        if(udpMarginQueue.Count > 0)
        {
            sendUDPCommands();
        }
        
    }

}
