using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;

public class redis : MonoBehaviour
{
    public string foo;

    // Start is called before the first frame update
    void Start()
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("192.168.154.84:6379");
        ISubscriber subscriber = redis.GetSubscriber();

        subscriber.Subscribe("channel", (channel, message) =>
        {
            Debug.Log($"Received message: {message} from channel: {channel}");
        });

        IDatabase db = redis.GetDatabase();
        db.StringSet("foo", "bar");
        foo = db.StringGet("foo");
    }

    // Update is called once per frame
    void Update()
    {
        // Your update logic here
    }
}
