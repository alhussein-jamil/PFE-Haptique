using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

public class redis : MonoBehaviour
{
    public string foo;

    // Start is called before the first frame update
    void Start()
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("192.168.154.84:6379");
        IDatabase db = redis.GetDatabase();
        db.StringSet("foo", "bar");
        foo = db.StringGet("foo");
    }

    // Update is called once per frame
    void Update()
    {
        

    }
}
