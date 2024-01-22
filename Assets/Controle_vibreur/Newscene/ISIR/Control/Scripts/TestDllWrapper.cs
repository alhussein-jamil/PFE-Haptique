using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class TestDllWrapper : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        HapticManagerCEAWrapper.Instance.init(1, "COM8");
        Debug.Log("Current" + HapticManagerCEAWrapper.Instance.getBufferSize().ToString());

        byte[] array = new byte[16];

        for (int i = 0; i < 16; i++)
        {
            array[i] = (byte)(i + 97);
        }

        // Copy the array to unmanaged memory.
        HapticManagerCEAWrapper.Instance.nextSample(1, array);
        HapticManagerCEAWrapper.Instance.nextSample(2, array);
        array = new byte[16];

        for (int i = 0; i < 16; i++)
        {
            array[i] = (byte)(i + 98);
        }
        HapticManagerCEAWrapper.Instance.nextSample(3, array);
        HapticManagerCEAWrapper.Instance.nextSample(4, array);
        HapticManagerCEAWrapper.Instance.nextSample(5, array);
        HapticManagerCEAWrapper.Instance.nextSample(6, array);
        HapticManagerCEAWrapper.Instance.nextSample(7, array);
        HapticManagerCEAWrapper.Instance.nextSample(8, array);

        Debug.Log("Current" + HapticManagerCEAWrapper.Instance.getBufferSize().ToString());

    }

    void OnApplicationQuit()
    {
        HapticManagerCEAWrapper.Instance.close();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Current" + HapticManagerCEAWrapper.Instance.getBufferSize().ToString());
    }
}
