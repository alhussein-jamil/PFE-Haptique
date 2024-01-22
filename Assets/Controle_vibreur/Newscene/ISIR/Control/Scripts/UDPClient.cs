using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDPClient : SingletonBehaviour<UDPClient>
{

    UdpClient udpClient;
    public string ip = "127.0.0.1";
    public int port = 55555;

    protected override bool Awake()
    {
        if (base.Awake())
        {
            //initialize here
            return true;
        }
        else
            return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        udpClient = new UdpClient();
        System.Net.IPEndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port); // endpoint where server is listening
        udpClient.Connect(ep);
    }

    // Update is called once per frame
    public void SendData(byte[] data)
    {
        if(udpClient != null)
        {
            udpClient.Send(data, data.Length);
        }
    }
}
