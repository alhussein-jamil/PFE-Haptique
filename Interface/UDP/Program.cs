// See https://aka.ms/new-console-template for more information
UDPManagerRedis udpManager = new UDPManagerRedis();
UDPRedisClient udpRedisClient = new UDPRedisClient(udpManager);
udpRedisClient.Start();