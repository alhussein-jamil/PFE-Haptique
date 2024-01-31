using System;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: YourProgram.exe <IP_Address>");
            return;
        }

        string ip_adress = args[0];

        // Rest of your code
        UDPManagerRedis udpManager = new UDPManagerRedis();
        UDPRedisServer uDPRedisServer = new UDPRedisServer(udpManager, ip_adress);
        uDPRedisServer.Start();
        RobotSimulator robotSimulator = new RobotSimulator(uDPRedisServer.redis, uDPRedisServer.subscriber, uDPRedisServer.publisher);

        Console.WriteLine(uDPRedisServer.redis.IsConnected);
        // robotSimulator.Start();
        // Console.WriteLine("RobotSimulator started");

        // wait indefinitely
        Console.WriteLine("Press any key to exit...");

        // Continue with the rest of your code on the main thread
        // ...
    }
}
