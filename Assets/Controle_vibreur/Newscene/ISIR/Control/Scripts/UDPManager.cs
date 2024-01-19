using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static DebugLog;

public class UDPManager : SingletonBehaviour<UDPManager>
{
    public e_LogLvl debug = e_LogLvl.DEBUG;

    // receiving Thread
    Thread receiveThread;
    private bool running = false;
    public readonly object comLock = new object();
    // udpclient object
    UdpClient udpClient;
    UdpClient udpClientR;
    public string ip = "127.0.0.1";
    public int portSender = 26000;
    public int portListener = 26001;

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

    void Start()
    {
        DebugLog.Instance.setDebug(this.GetType().ToString(), debug);
    }

    //Use to validate the field
    private void OnValidate()
    {
        if (Application.isPlaying && DebugLog.Instance != null)
        {
            DebugLog.Instance.setDebug(this.GetType().ToString(), debug);
        }
    }

    void OnApplicationQuit()
    {
         DebugLog.Instance.Log(this.GetType().ToString(), "OnApplicationQuit");
        closeConnetion();
    }
   
    [ContextMenu("Start Connection UDP")]
    public void StartCom()
    {
        if (running) return; //Start only once
        initConnection();
    }

    private void closeConnetion()
    {
        lock (comLock)
        {
            if (running)
            {
                running = false;
                udpClient.Close();
                udpClientR.Close();
                 DebugLog.Instance.Log(this.GetType().ToString(), "Stop sending thread"); // Just Checking ^^
            }
        }
    }

    private void initConnection()
    {
        lock(comLock)
        {
            udpClient = new UdpClient();
            udpClientR = new UdpClient(portListener);
            System.Net.IPEndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), portSender); // endpoint where server is listening
            udpClient.Connect(ep);

            running = true;
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
    }

    // Update is called once per frame
    public void SendData(byte[] data)
    {
        lock (comLock)
        {
            if (running)
            {
                udpClient.Send(data, data.Length);
            }
        }
    }

    // receive thread
    private void ReceiveData()
    {
         DebugLog.Instance.Log(this.GetType().ToString(), "Start received");
        running = true;
        udpClientR.Client.ReceiveTimeout = 5000;
        while (running)
        {
            try
            {
                var remoteEP = new IPEndPoint(IPAddress.Any, portListener);
                byte[] data = udpClientR.Receive(ref remoteEP);
                if (data != null)
                {
                    RaiseDataReceived(data);
                     DebugLog.Instance.Log(this.GetType().ToString(), "Server: " + Utils.ConvertByteArrayToString(data));
                }
            }
           
            catch (Exception err)
            {
                 DebugLog.Instance.Log(this.GetType().ToString(), err.ToString());
            }
        }
         DebugLog.Instance.Log(this.GetType().ToString(), "Stop received");
    }

    // Declare the delegate design to handle data reception.
    public delegate void UDPDataReceivedHandler(object sender, byte[] data);

    /// <summary>
    /// Occurs when new alert is detected.
    /// </summary>
    public event UDPDataReceivedHandler dataReceived;

    /// <summary>
    /// raise the alert event
    /// </summary>
    private void RaiseDataReceived(byte[] data)
    {
        // Raise the event by using the () operator.
        if (dataReceived != null)
            dataReceived(this, data);
    }

}
