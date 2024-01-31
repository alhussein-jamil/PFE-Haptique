using System.Collections.Generic;
using System.IO;
using System;
using StackExchange.Redis;
using System.Threading;
using System;
using System.Diagnostics;
using System.Threading;
public class RobotSimulator
{
    List<Tuple<double, double, double, double, double, double, double>> poss = new List<Tuple<double, double, double, double, double, double, double>>();

    public string filePath = "a.pos";
    public float file_caresse_speed = 0.5f;
    public float caresse_speed_scale = 1f;
    public float frequency = 100f; // 1000 Hz
    public int idx = 0;
    public bool _Moving = false;
    private bool subscribed;
    private ConnectionMultiplexer redis;
    private ISubscriber subscriber;
    private ISubscriber publisher;
    private RedisChannel encoders = new RedisChannel("sim_encoder_positions", RedisChannel.PatternMode.Auto);
    public RobotSimulator(ConnectionMultiplexer redis, ISubscriber subscriber, ISubscriber publisher)
    {
        this.redis = redis;
        this.subscriber = subscriber;
        this.publisher = publisher;
    }
    
    static void SetInterval(Action callback, double intervalInSeconds)
    {
        // Calculate the interval in milliseconds
        int interval = (int)(intervalInSeconds * 1000);

        // Create a Stopwatch to measure elapsed time
        Stopwatch stopwatch = new Stopwatch();

        // Start the stopwatch
        stopwatch.Start();

        // Infinite loop to simulate the function being called at the specified frequency
        while (true)
        {
            // Check if the elapsed time is greater than or equal to the desired interval
            if (stopwatch.ElapsedMilliseconds >= interval)
            {
                // Call the provided callback function
                callback.Invoke();

                // Reset the stopwatch
                stopwatch.Restart();
            }

            // Optional: Add a small delay to avoid high CPU usage
            Thread.Sleep(1);
        }
    }
    public void Start()
    {
        System.Console.WriteLine("Starting RobotSimulator");
        ReadData();
        // wait until redis is connected
        while (!redis.IsConnected)
        {
            Thread.Sleep(100);
        }
        SubscribeToCaresse();
        // call publish at 1000Hz
        SetInterval(() => PublishData(), 1.0f / frequency); 
    }

    public static List<byte> CoordsToLine(double[] coords)
        {
            List<byte> ret = new List<byte>();

            foreach (var coord in coords)
            {
                byte[] bytes = BitConverter.GetBytes(coord);
                ret.AddRange(bytes);
            }

            return ret;
        }
    private void ReadData(){
        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                double[] pos = new double[7];

                for (int i = 0; i < 7; i++)
                {
                    byte[] bytes = reader.ReadBytes(sizeof(double));
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    pos[i] = BitConverter.ToDouble(bytes, 0);
                }

                poss.Add(new Tuple<double, double, double, double, double, double, double>(
                    pos[0], pos[1], pos[2], pos[3], pos[4], pos[5], pos[6]));
            }
        }
    }

    private void SubscribeToCaresse(){
        System.Console.WriteLine("Subscribing to caresse");
            subscriber.Subscribe(new RedisChannel("caresse", RedisChannel.PatternMode.Auto), (channel, message) =>
            {

                System.Console.WriteLine("Received message from caresse " + message.ToString());
                _Moving = false;
                //wait for 1 second
                System.Threading.Thread.Sleep(150);
                string line = message.ToString();
                _Moving = true;
            });
    }
    private void PublishData()
    {
        if (redis.IsConnected)
        {
                int currentIdx = (int)(idx * caresse_speed_scale * 1000 / frequency);
                System.Console.WriteLine("Publishing data " + currentIdx);  
                if (currentIdx >= poss.Count)
                {
                    _Moving = false;
                    System.Console.WriteLine("Finished");
                    return;
                }   

                Tuple<double, double, double, double, double, double, double> pos = poss[currentIdx];
                double[] posArray = new double[7] { pos.Item1, pos.Item2, pos.Item3, pos.Item4, pos.Item5, pos.Item6, pos.Item7 };
                byte[] bytes = CoordsToLine(posArray).ToArray();
                publisher.Publish(encoders, bytes);
                if(_Moving)
                {
                    if (currentIdx < poss.Count - 1)
                    idx += 1;
                    else
                    _Moving = false;
                }

        }

    }

}
