using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace Franka{
    
public class SimRealRobot : MonoBehaviour
{
    List<Tuple<double, double, double, double, double, double, double>> poss = new List<Tuple<double, double, double, double, double, double, double>>();

    public string filePath = "a.pos";
    private RedisConnection redisConnection;
    public GameObject GameManager;
    public float frequency = 1000f; // 1000 Hz
    public int idx = 0;
    public bool Moving = false;
    private bool _Moving = false;
    private void Start()
    {
        if (GameManager == null)
                GameManager = GameObject.Find("GameManager");
        redisConnection = GameManager.GetComponent<RedisConnection>();
        _Moving = Moving;
        ReadData();
        // Start invoking the method with the specified interval
        InvokeRepeating("PublishData", 0f, 1f / frequency);
        
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
    private void PublishData()
    {
        if (redisConnection.doneInit)
        {
                Tuple<double, double, double, double, double, double, double> pos = poss[idx];
                double[] posArray = new double[7] { pos.Item1, pos.Item2, pos.Item3, pos.Item4, pos.Item5, pos.Item6, pos.Item7 };
                byte[] bytes = RedisConnection.CoordsToLine(posArray).ToArray();
                redisConnection.publisher.Publish(redisConnection.redisChannels["sim_encoder_positions"], bytes);
                if(_Moving)
                {
                    if (idx < poss.Count - 1)
                    idx ++;
                }

        }

    }
    private void Update()
    {
        if (Moving != _Moving)
        {
            _Moving =    Moving;
            if (Moving)
            {
                idx = 0;
            }
        }
    }


}

}
