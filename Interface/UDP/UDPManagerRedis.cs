using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPManagerRedis 
{

    public const string SFRAME_UDPHEADER_R = "$d";

	private int correctionTimingUDP = 0;
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

    protected bool Awake()
    {

            //initialize here
            return true;
    }

    void Start()
    {
        // DebugLog.Instance.setDebug(this.GetType().ToString(), debug);
    }

    //Use to validate the field
    private void OnValidate()
    {
 
    }

    void OnApplicationQuit()
    {
        closeConnetion();
    }
   

    public void StartCom()
    {
        if (running) return; //Start only once
        initConnection();
    }
    public void OnUDPMarginQueueReceived(object sender, byte[] d)
    {
        parseUDPFrame(d);
    }

	private void parseUDPFrame(byte[] data)
	{
        if (data.Length == 0) return;
		int parsingState = 0;
		byte[] header = Encoding.ASCII.GetBytes(SFRAME_UDPHEADER_R);
		int i = 0;
        while (i < data.Length) 
        {
			byte b = data[i];
            switch (parsingState) {
				case 0: // Header part 1
					i++;
                    if (b == header[0]) 
					{
						parsingState++;
                    }
					break;
                case 1: // Header part 2
                    if (b == header[1]) 
                    {
                        parsingState++;
                        i++;
                    }
                    else
					{
						parsingState = 0;
                    }
                    break;
                case 2: // value 
                    i++;
                    int value = ConvertByteToInt(b);
					correctionTimingUDP = value-100;
					parsingState = 0;
                    break;
            }
        }
    }
    	public static int ConvertByteToInt(byte value)
	{
		try
		{
			return System.Convert.ToInt32(value);
		}
		catch (System.OverflowException)
		{
			System.Console.WriteLine("The {0} value {1} can't be parse in interger type.", value.GetType().Name, value);
			return 0;
		}
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
                }
            }
           
            catch (Exception err)
            {
            }
        }
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
