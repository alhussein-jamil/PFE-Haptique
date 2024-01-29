using Franka;
using UnityEngine;

public class UDPRedisClient : MonoBehaviour
{
    public GameObject gameManager;
    public RedisConnection redisConnection;
    public bool subscribed = false;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager");   
        redisConnection = gameManager.GetComponent<RedisConnection>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!subscribed && redisConnection.doneInit)
        {
            var x = redisConnection.subscriber.Subscribe(redisConnection.redisChannels["haptic_udp"]);

            x.OnMessage(message => {
                string msg = message.Message.ToString();
                if(msg == "start")
                    UDPManagerRedis.Instance.StartCom();
                else if(msg == "bind")
                {
                    UDPManagerRedis.Instance.dataReceived += UDPManagerRedis.Instance.OnUDPMarginQueueReceived;
                }
                UDPManagerRedis.Instance.SendData(message.Message);
            });
            

            subscribed = true;
        }
        
    }
}
