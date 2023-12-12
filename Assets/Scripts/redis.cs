using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StackExchange.Redis;

public class Redis : MonoBehaviour
{
    public string foo;

    // Start is called before the first frame update
    void Start()
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("192.168.154.84:6379");
        IDatabase db = redis.GetDatabase();
        db.StringSet("foo", "bar");
        foo = db.StringGet("foo");

        // Creating a subscriber
        ISubscriber subscriber = redis.GetSubscriber();

        // Subscribe to a subject
        subscriber.Subscribe(new RedisChannel("Robot_Encoders", RedisChannel.PatternMode.Auto), (channel, message) =>
        {
            Debug.Log($"Received message from {channel}: {message}");
        });

        // Publishing to another subject
        ISubscriber publisher = redis.GetSubscriber();
        publisher.Publish("Robot_Encoders", "Hello World");
    }

    // Update is called once per frame
    void Update()
    {
        // Your update logic here
    }
}
