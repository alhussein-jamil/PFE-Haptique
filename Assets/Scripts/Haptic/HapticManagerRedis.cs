using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using System;
using Franka;
using System.IO;

class RedisUDPTunnel
{
	private RedisConnection redisConnection;


	public RedisUDPTunnel(RedisConnection redis)
	{
		redisConnection = redis;
	}
	public void StartCom()
	{

		redisConnection.publisher.Publish(redisConnection.redisChannels["haptic_udp"], "start");
Debug.Log("lestart");
	}

	public void SendData(byte[] d)
	{

		redisConnection.publisher.Publish(redisConnection.redisChannels["haptic_udp"], d);
	}
    internal void BindHandle()
    {
        redisConnection.publisher.Publish(redisConnection.redisChannels["haptic_udp"], "bind");
    }
}

public class HapticManagerRedis : SingletonBehaviour<HapticManager>
{
	public enum e_base_frequency
	{
		VIB_DEVICE_FREQUENCY = 0,
		GRIP_DEVICE_FREQUENCY
	}
    public enum e_com_system
    {
        SERIAL = 0,
        UDP,
        DLL,
        DLL_FIXED
    }
    public e_com_system comSystem= e_com_system.UDP;
    public bool useConfigurationFile = false;
    [SerializeField]
	private string _configurationFile = "globalSetting.json";
	private ConfigFileJSON config; //Contain the configuration

	private Thread t_threadSend;                    //Thread use to receive incomming message from client
	private bool running = false;
	private ulong frameID = 0;
    private ulong unityFrameID = 0;
    [Header("Vibrotactile Device Parameters")]
	public bool autoStartVibDevice = true;
	public string vibrotactileDeviceName = "VibrotactileDevice";
    [Delayed]
    public float frequencyVib = 4000f;
	private int correctionTiming = 0; // [-100, 100]	gap in sample between the write cursor and the read cursor of the STM, allow to speed up or slow down the sending of data
	private int correctionTimingUDP = 0; //  [-100, 100] number of sample in less or more than the margin MARGE_QUEUE
    [Range(2, 128)]
    [Delayed]
    public int sampleNumber = 32; // number of sample send to controler (this parameter is send in the frame)
	private float securityDelay = 10000; //If no data are send between this delay (in ms), stop the thread for security purpose, this is probably because the application is stop but not the thread. value should be ~ =1000000 * sampleNumber / frequencyVib;
    [Range(1,28)]
    [Delayed]
    public int numberOfChannel = 28; // number of channel send to controler (this parameter is send in the frame)
    [Range(1,511)]
	public int maxPWM = 511; //Max value of the PWM, depending on the maxPWM the sample value is encoded on 8bits(1byte) (if < 255) or 16bits(2bytes)

    public List<HapticDevice> hapticDevices =  new List<HapticDevice>();

	[Header("Grip Device Parameters")]
	public bool autoStartGripDevice = true;
	public RedisConnection redisConnection;
	public string gripDeviceName = "GripDevice";
	public float frequencyGrip = 500f;
	[Range(1, 255)]
	public int maxSpeed = 191;
	[Range(1, 255)]
	public int maxForce= 255;

	public HapticDeviceGrip hapticGripDevice;
	private PWMBalanceWatcher pwmWatcher;
	public UDPManager uDPManager;
	//CONST VIB
	public const string SFRAME_NOSPLIT = "$A";
	public const string SFRAME_SPLIT = "$B";
	public const string SFRAME_UDPHEADER = "$D";
	public const string SFRAME_UDPHEADER_R = "$d";
    public const string TRAME_HEAD_16bits = "C";
	public const string TRAME_HEAD_8bits = "c";
	public const int HEADER_NOSPLIT_SIZE = 2; //2byte for Header
	public const int HEADER_SPLIT_SIZE = 4; //2byte for header + 2byte for size
	public const int MAX_SIZE = 64; //max value possible
	public const float COEFF_TIMING = 1.0f; //additional delay for sending thread in ms
	public const float GAIN_CORRECTION = 0.0005f; //gain to compute the correction of sending frequency
	public const float GAIN_QUEUE = 0.5f;
	public const float GAIN_QUEUE_python = 1.0f; //#-> pour convertir en uint8_t
	public const float MARGE_QUEUE = 20;

	//CONST GRIP
	public const string SFRAME_SPEED = "$V";
	public const string SFRAME_FORCE = "$F";
	public const int HEADER_GRIP_SIZE = 2; //2byte for Header
	private RedisUDPTunnel redisUDPTunnel;
	public GameObject gameManager;

    public float getFrequency(e_base_frequency baseF)
    {
		return baseF == e_base_frequency.VIB_DEVICE_FREQUENCY ? HapticManager.Instance.frequencyVib : HapticManager.Instance.frequencyGrip;
	}

	
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

    //Use to validate the field
    private void OnValidate()
    {
        sampleNumber = Utils.ToNearestPowerOfTwo(sampleNumber);
        securityDelay = 5000000 * sampleNumber / frequencyVib;
        if (Application.isPlaying)
        {
        }
    }


    private void Update()
    {
        unityFrameID++;
    }

    private void FixedUpdate()
    {
       if(comSystem ==e_com_system.DLL_FIXED)
		{
			int nbData = Mathf.CeilToInt(Time.fixedDeltaTime * 1000f / (float)getPeriodeVib());
			nbData = nbData + Mathf.CeilToInt(nbData * 0.5f);
			uint bufferDll = HapticManagerCEAWrapper.Instance.getBufferSize();

            nbData = Mathf.Max((int)(nbData - bufferDll), 0);
            for (int i = 0; i < nbData; i++)
			{
				byte[] data = getNextVibSamples(sampleNumber);
				HapticManagerCEAWrapper.Instance.nextSample(frameID, data);
            }
            // DebugLog.Instance.Log(this.GetType().ToString(), "Missing Data" + HapticManagerCEAWrapper.Instance.getMistData().ToString());
       }
    }

    // Start is called before the first frame update
    void Start()
    {	
		gameManager = GameObject.Find("GameManager");
		redisConnection = gameManager.GetComponent<RedisConnection>();
		
		redisUDPTunnel = new RedisUDPTunnel(redisConnection);
		unityFrameID = 0;
        //  DebugLog.Instance.Log(this.GetType().ToString(), "Main Cpu used > " + GetCurrentProcessorNumber()); // Just Checking ^^
		if(useConfigurationFile)
		{
			configure();
		}
        securityDelay = 5000000 * sampleNumber / frequencyVib;

        pwmWatcher = new PWMBalanceWatcher(maxPWM, numberOfChannel);
        pwmWatcher.AlertDetected += PwmWatcher_AlertDetected;
		if(comSystem != e_com_system.DLL_FIXED)
        {
            t_threadSend = new Thread(t_sendMessage)
            {
                // According with the Official documentation : https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity8.html
                // ' The priority for the main thread and graphics thread are both ThreadPriority.Normal.
                //   Any threads with higher priority preempt the main/graphics threads and cause framerate hiccups,
                //   whereas threads with lower priority do not. If threads have an equivalent priority to the main thread, the CPU attempts to give equal time to the threads,
                //   which generally results in framerate stuttering if multiple background threads are performing heavy operations, such as AssetBundle decompression.'
                // So, better use a lowest Thread Priority
                Priority = System.Threading.ThreadPriority.Normal, //// Lowest, BelowNormal, Normal, AboveNormal, Highest
                IsBackground = true
            };
            t_threadSend.Start();
			running = true;
		}

		if(comSystem == e_com_system.SERIAL)
		{
			if(autoStartVibDevice)
				SerialCOMManager.Instance.StartCom(vibrotactileDeviceName);
			if(autoStartGripDevice)
				SerialCOMManager.Instance.StartCom(gripDeviceName);
		}
        else if (comSystem == e_com_system.UDP)
        {
			if (autoStartVibDevice)
			{
				//uDPManager.StartCom();
				redisUDPTunnel.StartCom();

				//uDPManager.dataReceived += OnUDPMarginQueueReceived;
				redisUDPTunnel.BindHandle();
			}
        }
        else if(comSystem == e_com_system.DLL || comSystem == e_com_system.DLL_FIXED)
        {
            string portName = SerialCOMManager.Instance.getDeviceParameter(vibrotactileDeviceName).Port;
            HapticManagerCEAWrapper.Instance.init((int)frequencyVib/sampleNumber, portName);
		}
    }

    private void OnUDPMarginQueueReceived(object sender, byte[] d)
    {
        parseUDPFrame(d);
    }
    private void PwmWatcher_AlertDetected(object sender, PWMBalanceWatcher.AlertPWMBalanceArgs e)
    {
        if (e.channel < hapticDevices.Count && hapticDevices[e.channel] != null)
		{
            hapticDevices[e.channel].Active = false;
        }
    }

    void OnApplicationQuit()
    {
        setGripConst(0);

		if (running)
        {
            running = false;
            t_threadSend.Join();
        }
        if (comSystem == e_com_system.DLL || comSystem == e_com_system.DLL_FIXED)
        {
            HapticManagerCEAWrapper.Instance.close();
        }
    }

	private void setGripConst(float v)
    {
        if (hapticGripDevice)
		{
			byte[] b = new byte[1];
			b[0] = Utils.ConvertToPWM(v, hapticGripDevice.GripCtrl == HapticDeviceGrip.e_gripCtrl.SPEED_CTRL ? maxSpeed : maxForce, false);
			// DebugLog.Instance.Log(this.GetType().ToString(), "GRI B :"+ v +" " + b[0]);
			b = generateGripFrames(b, hapticGripDevice.GripCtrl);
		   /* foreach (var item in b)
			{
				 DebugLog.Instance.Log(this.GetType().ToString(), "B final :"+ item);
			}*/
			SerialCOMManager.Instance.SendCOMMessage(gripDeviceName, b);
		}
		else
		{
		}
		
	}
	private void t_sendMessage()
	{
		int cpumask = 1 << (5 % System.Environment.ProcessorCount);
        // ************************************************************* Windows 32/64
        // SetThreadAffinityMask(GetCurrentThread(), cpumask); // thread=>cpu
        //  DebugLog.Instance.Log(this.GetType().ToString(), "Send Thread n� Cpu used > " + GetCurrentProcessorNumber()); // Just Checking 
        // // ************************************************************* Windows 32/64
		frameID = 0;
		ulong lastUnityFrameID = 0; //Use for security check if unity still runing
		bool vibIsMinPeriode = getPeriodeVib() < getPeriodeGrip() || hapticGripDevice == null;		
		Stopwatch swMainThread = Stopwatch.StartNew();
		Stopwatch swUnitySecurity = Stopwatch.StartNew();

		List<byte[]> frames = null;
		byte[] data = null;

        swMainThread.Start();
        while (running) //while the server is running
		{
			//Security test
            if (lastUnityFrameID != unityFrameID) { swUnitySecurity.Restart(); }
            lastUnityFrameID = unityFrameID;
            if (swUnitySecurity.Elapsed.TotalMilliseconds > securityDelay) //Stop thread after a long time without unity frame (prevent lost thread)
            {
                running = false;
                continue;
            }

			if (comSystem == e_com_system.DLL)
			{
                uint bufferDll = HapticManagerCEAWrapper.Instance.getBufferSize();
                if (bufferDll <= 10)
				{
                    data = getNextVibSamples(sampleNumber);
                    HapticManagerCEAWrapper.Instance.nextSample(frameID, data);
                }
				if (bufferDll <= 3)
				{
					data = getNextVibSamples(sampleNumber);
					HapticManagerCEAWrapper.Instance.nextSample(frameID, data);
				}
            }
            else //SERIAL | UDP
            {
                data = getNextVibSamples(sampleNumber);
                
                if (comSystem == e_com_system.UDP)
                {
                    //uDPManager.SendData(Encoding.ASCII.GetBytes(SFRAME_UDPHEADER)); //UDP HEADER
					redisUDPTunnel.SendData(Encoding.ASCII.GetBytes(SFRAME_UDPHEADER)); //UDP HEADER
                    //uDPManager.SendData(data);
					redisUDPTunnel.SendData(data);


                }
                else
                {
                    frames = generateVibFrames(data, false);
					foreach (var fra in frames)
					{
						SerialCOMManager.Instance.SendCOMMessage(vibrotactileDeviceName, fra);

						//wait correct time
						int[] rep = SerialCOMManager.Instance.ReadAllCOM(vibrotactileDeviceName);
						if (rep.Length > 0 && rep[rep.Length - 1] >= 0)
						{
							correctionTiming = Mathf.Clamp((correctionTiming + rep[rep.Length - 1] - 100), -100, 100);
						}
					}
				}
            }

            //wait for next round
            while (swMainThread.Elapsed.TotalMilliseconds < getPeriodeVib()*(double)frameID) { };

            /////
            //TODO reimplementer Grip
            /*if (hapticGripDevice != null && countdownGrip >= getPeriodeGrip())
			{
				//if (!vibIsMinPeriode) { frameID++; } //TODO add grip (grip should have is proper frameId maybe
                byte[] data = getNextGripSamples(1);
                data = generateGripFrames(data, hapticGripDevice.GripCtrl);
                SerialCOMManager.Instance.SendCOMMessage(gripDeviceName, data);
                countdownGrip = 0;
			}*/
            // DebugLog.Instance.Log(this.GetType().ToString(), "send duration: " + (swTest.Elapsed.TotalMilliseconds - lastTime));
        }
        swMainThread.Stop();
    }

    //return the periode of the vib loop in ms
    private double getPeriodeVib()
    {
		if (comSystem == e_com_system.UDP) //(time.perf_counter()-t0<(1/fe)*LEN_USBBUFF*(1.0+GAIN_QUEUE_unity*(correctionTimingUDP/GAIN_QUEUE_python)/MARGE_QUEUE))
            return (1.0d / frequencyVib) * sampleNumber * (1.0d + GAIN_QUEUE*(correctionTimingUDP / GAIN_QUEUE_python) / MARGE_QUEUE) * 1000.0d;
        else
			return (1.0d / frequencyVib) * sampleNumber * (COEFF_TIMING + GAIN_CORRECTION * correctionTiming)  * 1000.0d; 
    }

	//return the periode of the grip loop in ms
	private double getPeriodeGrip()
	{
		return 1f / frequencyGrip * 1000f;
	}

	int iter = 0; //Debug

	private byte[] getNextVibSamples(int size, bool updateIdFrame = true)
    {
		string debugStr = "Samples Vib :";
		int sizeEncodingPWMValue = maxPWM <= 255 ? 1 : 2; //depending on the maxPWM the value is encode on 8bits(1byte) or 16bits(2bytes)
        int trameExtraByte = 1 + 1; //   1byte for  parameter(Len_USBBuff & numberOfChannel) +  1byte TRAME_HEAD_8/16Bits + 1byte for Trigger 
		int paramExtraByte = 1;
        int sizeEncodingValue = numberOfChannel * sizeEncodingPWMValue; //Each pwm value(numChannel) on 1 or 2 byte (8/16bits encoding)
        byte[] nxtSamples = new byte[size * (sizeEncodingValue + trameExtraByte) + paramExtraByte];
		byte[] zeroVal = Utils.ConvertToLargePWM(0, maxPWM, true, sizeEncodingPWMValue); //default value if no HapticDevices
        // add parameters (Len_USBBuff & numberOfChannel) 
        int iParam = (Mathf.CeilToInt(Mathf.Log(size, 2) - 1)) << 5;
		byte param = Utils.ConvertIntToByte(iParam | (numberOfChannel & 0b11111));  //--decode : size= data & 0b11111
        nxtSamples[0] = param;
        for (int i = 0; i< size; i++)
		{
			debugStr += "\n => " + Encoding.ASCII.GetBytes(sizeEncodingPWMValue == 1 ? TRAME_HEAD_8bits : TRAME_HEAD_16bits)[0].ToString() + " " + Utils.ConvertIntToByte(iter);
            // Header 8/16 bit
            nxtSamples[i * (sizeEncodingValue + trameExtraByte) + paramExtraByte] = Encoding.ASCII.GetBytes(sizeEncodingPWMValue == 1 ? TRAME_HEAD_8bits : TRAME_HEAD_16bits)[0]; //Debug
            // Trigger Bit
            // nxtSamples[i * (numberOfChannel+2)] = 0; //Synchro byte (not use)
            nxtSamples[i * (sizeEncodingValue + trameExtraByte) + 1 + paramExtraByte] = Utils.ConvertIntToByte(iter); //Debug else put 0
			iter++; //Debug
			if (iter > 255) //Debug
            { iter = 0; } //Debug
            for (int j = 0; j < numberOfChannel ; j++)
			{
				float sample = 0;
                byte[] val = zeroVal; //default value if no HapticDevices
                if (j < hapticDevices.Count && hapticDevices[j] != null)
                {
					sample = hapticDevices[j].getSamples(frameID, size)[i]; 
                    val = Utils.ConvertToLargePWM(sample, maxPWM, true, sizeEncodingPWMValue);
                }
				pwmWatcher.addValue(j, sample);
				debugStr += "(" + j + ")";
				for(int s=0; s <sizeEncodingPWMValue; s++)
				{
                    nxtSamples[i * (sizeEncodingValue + trameExtraByte) + j*sizeEncodingPWMValue + trameExtraByte + s + paramExtraByte] = val[s];
                    debugStr +=  val[s].ToString() + "|";

                }
                debugStr += ",";
            }
            pwmWatcher.addframe();

        }

        if (updateIdFrame) { frameID++; }


        return nxtSamples;
    }

	private byte[] getNextGripSamples(int size)
	{
		string debugStr = "Samples Grip:";
		byte[] nxtSamples = new byte[size];
		for (int i = 0; i < size; i++)
		{
			debugStr += " =>";
			byte val = Utils.ConvertToPWM(0, maxPWM, false);
			if (hapticGripDevice != null)
            {
				val = Utils.ConvertToPWM(hapticGripDevice.getSamples(frameID, 1)[0], hapticGripDevice.GripCtrl == HapticDeviceGrip.e_gripCtrl.SPEED_CTRL? maxSpeed :  maxForce, false);
				nxtSamples[i] = val;
				debugStr += val.ToString() + ",";
			}
		}
		
		return nxtSamples;
	}
    //Create a Frames from byte (add header and split if too big)
    public List<byte[]> generateVibFrames(byte[] value, bool uniqueSend = false)
	{
        int L = value.Length;
		List<byte[]> frames = new List<byte[]>();
		List<byte> tabchar = new List<byte>();
		if (L > 2058) //Max lenght managed by the STM32
		{
		}
		//depending of the size of data
		if (L <= MAX_SIZE - HEADER_NOSPLIT_SIZE)
		{
			tabchar.AddRange(Encoding.ASCII.GetBytes(SFRAME_NOSPLIT));
			tabchar.AddRange(value);
			frames.Add(tabchar.ToArray());
		}
		else
		{
			//Add Header
			tabchar.AddRange(Encoding.ASCII.GetBytes(SFRAME_SPLIT));
			//Add size // other possibility  Convert.ToByte(((L >> 8) & 0xff)); Convert.ToByte(L & 0xff);
			byte[] lenb = Utils.ConvertIntToBytes(L, 2);
			tabchar.AddRange(lenb);
			//Add Data
			if (uniqueSend)
			{
				tabchar.AddRange(value);
				frames.Add(tabchar.ToArray());

            } else
			{
				//Add Data
				tabchar.AddRange(Utils.SubArray(value, 0, MAX_SIZE - HEADER_SPLIT_SIZE));
	
				frames.Add(tabchar.ToArray());
				tabchar.Clear();
				
				//Create a new frame for data remaining
				for (int i = 0; i < (L - (MAX_SIZE - HEADER_SPLIT_SIZE)) / MAX_SIZE + 1; i++)
				{
					int len = (int)Mathf.Min(L - ((MAX_SIZE - HEADER_SPLIT_SIZE) + MAX_SIZE * i), MAX_SIZE);
					tabchar.AddRange(Utils.SubArray(value, (MAX_SIZE - HEADER_SPLIT_SIZE) + MAX_SIZE * i, len));
	
					frames.Add(tabchar.ToArray());
					tabchar.Clear();
				}
			}
        }

		return frames;
	}

	public byte[] generateGripFrames(byte[] value, HapticDeviceGrip.e_gripCtrl typeCtrl)
	{ 
		List<byte> tabchar = new List<byte>();
		tabchar.AddRange(Encoding.ASCII.GetBytes(typeCtrl==HapticDeviceGrip.e_gripCtrl.SPEED_CTRL? SFRAME_SPEED : SFRAME_FORCE));
		tabchar.Add((byte)1);//dir =1 -> fermeture  =0 ouverture
		tabchar.AddRange(value);
		return tabchar.ToArray();
	}

	private void configure()
	{
		_configurationFile ="Assets/Resources/" + _configurationFile;
		// Check if the file exist
		if (!File.Exists(_configurationFile))
		{
			throw new FileNotFoundException($"File not found: {_configurationFile}");
		}
		PlayerPrefs.SetString("configurationFile", _configurationFile);
		config = new ConfigFileJSON(_configurationFile,true);
		if (config.Parameters.HasKey("global_setting")  && config.Parameters["global_setting"]["overwrite_data"] != null && config.Parameters["global_setting"]["overwrite_data"].AsBool)
		{
			if (config.Parameters["global_setting"]["debug"] != null)
			{
            }
            if (config.Parameters["global_setting"]["com_system"] != null)
            {
                string param = config.Parameters["global_setting"]["com_system"];
                comSystem = Utils.getEnumFromString<e_com_system>(param, e_com_system.SERIAL);
            }
           
        }

		if (config.Parameters.HasKey("vibrotactile_device_setting") && config.Parameters["vibrotactile_device_setting"]["overwrite_data"] != null && config.Parameters["vibrotactile_device_setting"]["overwrite_data"].AsBool)
		{
			vibrotactileDeviceName = config.Parameters["vibrotactile_device_setting"]["device_name"];
			SerialParameter sp = SerialCOMManager.Instance.getDeviceParameter(config.Parameters["vibrotactile_device_setting"]["device_name"]);
			if (sp == null) // If Device not exist, create one
			{
				sp = new SerialParameter();
				SerialCOMManager.Instance.devicesParameters.Add(sp);
			}
			if (config.Parameters["vibrotactile_device_setting"]["device_name"] != null)
			{
				sp.DeviceName = config.Parameters["vibrotactile_device_setting"]["device_name"];
				vibrotactileDeviceName = sp.DeviceName;
            }
			if (config.Parameters["vibrotactile_device_setting"]["host_port"] != null)
			{
				sp.Port = config.Parameters["vibrotactile_device_setting"]["host_port"];
			}
			if (config.Parameters["vibrotactile_device_setting"]["host_speed"] != null)
			{
				sp.Speed = config.Parameters["vibrotactile_device_setting"]["host_speed"];
			}
			if (config.Parameters["vibrotactile_device_setting"]["auto_start"] != null)
			{
				autoStartVibDevice = config.Parameters["vibrotactile_device_setting"]["auto_start"].AsBool;
			}
            if (config.Parameters["vibrotactile_device_setting"]["frequency"] != null)
            {
                frequencyVib = config.Parameters["vibrotactile_device_setting"]["frequency"].AsInt;
            }
            if (config.Parameters["vibrotactile_device_setting"]["sample_number"] != null)
            {
                sampleNumber = config.Parameters["vibrotactile_device_setting"]["sample_number"].AsInt;
            }
            if (config.Parameters["vibrotactile_device_setting"]["nb_channel"] != null)
            {
                numberOfChannel = config.Parameters["vibrotactile_device_setting"]["nb_channel"].AsInt;
            }
            if (config.Parameters["vibrotactile_device_setting"]["max_pwm"] != null)
            {
                maxPWM = config.Parameters["vibrotactile_device_setting"]["max_pwm"].AsInt;
            }
        }

		if (config.Parameters.HasKey("grip_device_setting") &&  config.Parameters["grip_device_setting"]["overwrite_data"] != null && config.Parameters["grip_device_setting"]["overwrite_data"].AsBool)
		{
			gripDeviceName = config.Parameters["grip_device_setting"]["device_name"];
			SerialParameter sp = SerialCOMManager.Instance.getDeviceParameter(config.Parameters["grip_device_setting"]["device_name"]);
			if (sp == null) // If Device not exist, create one
			{
				sp = new SerialParameter();
				SerialCOMManager.Instance.devicesParameters.Add(sp);
			}
			if (config.Parameters["grip_device_setting"]["device_name"] != null)
			{
				sp.DeviceName = config.Parameters["grip_device_setting"]["device_name"];
				gripDeviceName = sp.DeviceName;
			}
			if (config.Parameters["grip_device_setting"]["host_port"] != null)
			{
				sp.Port = config.Parameters["grip_device_setting"]["host_port"];
			}
			if (config.Parameters["grip_device_setting"]["host_speed"] != null)
			{
				sp.Speed = config.Parameters["grip_device_setting"]["host_speed"];
			}
			if (config.Parameters["grip_device_setting"]["auto_start"] != null)
			{
				autoStartGripDevice = config.Parameters["grip_device_setting"]["auto_start"].AsBool;
			}
            if (config.Parameters["grip_device_setting"]["frequency"] != null)
            {
                frequencyGrip = config.Parameters["grip_device_setting"]["frequency"].AsInt;
            }
            if (config.Parameters["grip_device_setting"]["max_speed"] != null)
            {
                maxSpeed = config.Parameters["grip_device_setting"]["max_speed"].AsInt;
            }
            if (config.Parameters["grip_device_setting"]["max_force"] != null)
            {
                maxForce = config.Parameters["grip_device_setting"]["max_force"].AsInt;
            }
        }
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
                    int value = Utils.ConvertByteToInt(b);
					correctionTimingUDP = value-100;
					parsingState = 0;
                    break;
            }
        }
    }

}