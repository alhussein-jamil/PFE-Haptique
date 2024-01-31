

// See https://aka.ms/new-console-template for more information
UDPManagerRedis udpManager = new UDPManagerRedis();
UDPRedisServer uDPRedisServer = new UDPRedisServer(udpManager);
uDPRedisServer.Start();
RobotSimulator robotSimulator = new RobotSimulator(uDPRedisServer.redis, uDPRedisServer.subscriber, uDPRedisServer.publisher);
System.Console.WriteLine(uDPRedisServer.redis.IsConnected);
// robotSimulator.Start();
System.Console.WriteLine("RobotSimulator started");
// wait indefinitely
Thread.Sleep(Timeout.Infinite);