using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
//using HapticManagerLibrary;
using System.ComponentModel.Composition.Primitives;

public class TestDll : MonoBehaviour
{

    [DllImport("HapticManagerLibrary.dll")]
    private static extern void hapticDevice_init(int freq, string comPort);

    [DllImport("HapticManagerLibrary.dll")]
    private static extern bool hapticDevice_nextSample(ulong idFrame, IntPtr inputBuffer, int nSize);

    [DllImport("HapticManagerLibrary.dll")]
    private static extern uint hapticDevice_getBufferSize();

    [DllImport("HapticManagerLibrary.dll")]
    private static extern bool hapticDevice_close();
    public string comPort = "COM8";

    // Start is called before the first frame update
    void Start()
    {
        hapticDevice_init(1, comPort);
        Debug.Log("Current" + hapticDevice_getBufferSize().ToString());


        byte[] array = new byte[16];

        for (int i = 0; i < 16; i++)
        {
            array[i] = (byte)(i + 97);
        }

        int size = Marshal.SizeOf(array[0]) * array.Length;
        IntPtr pnt = Marshal.AllocHGlobal(size);

        try
        {
            // Copy the array to unmanaged memory.
            Marshal.Copy(array, 0, pnt, array.Length);
            hapticDevice_nextSample(1, pnt, array.Length);
        }
        finally
        {
            // Free the unmanaged memory.
            Marshal.FreeHGlobal(pnt);
            Debug.Log("Free");

        }
        Debug.Log("Current" + hapticDevice_getBufferSize().ToString());

        //hapticDevice_nextSample(2, array, array.Length);
        //Marshal.FreeHGlobal(pnt);
        Debug.Log("Current" + hapticDevice_getBufferSize().ToString());

        hapticDevice_close();
        //---------------
        //HapticManagerCEA.hapticDevice_init(1, 2);
        /*Debug.Log("Current#" + HapticManagerCEA.hapticDevice_getCurrent().ToString());
        byte[] array2 = new byte[16];

        for (int i = 0; i < 16; i++)
        {
            array2[i] = (byte)(i + 98);
        }

        HapticManagerCEA.hapticDevice_nextSample(array2, array2.Length, 1);
        Debug.Log("Current#" + HapticManagerCEA.hapticDevice_getCurrent().ToString());
        HapticManagerCEA.hapticDevice_nextSample(array2, array2.Length, 1);
        Debug.Log("Current#" + HapticManagerCEA.hapticDevice_getCurrent().ToString());
        HapticManagerCEA.hapticDevice_close();*/

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Current" + hapticDevice_getBufferSize().ToString());
    }
}
