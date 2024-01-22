//using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.PlayerLoop;


public class HapticManagerCEAWrapper : SingletonBehaviour<HapticManagerCEAWrapper>
{
    private bool initialized = false;

    [DllImport("HapticManagerLibrary.dll")]
    private static extern void hapticDevice_init(int freq, string comPort);

    [DllImport("HapticManagerLibrary.dll")]
    private static extern bool hapticDevice_nextSample(ulong idFrame, IntPtr inputBuffer, int nSize);

    [DllImport("HapticManagerLibrary.dll")]
    private static extern uint hapticDevice_getBufferSize();

    [DllImport("HapticManagerLibrary.dll")]
    private static extern uint hapticDevice_getMissingDataCount();


    [DllImport("HapticManagerLibrary.dll")]
    private static extern bool hapticDevice_close();

    public void init(int freq, string comPort)
    {
        initialized = true;
        hapticDevice_init(freq, comPort);
    }

    public bool nextSample(ulong idFrame, byte[] inputBuffer)
    {
        bool success = false;
        int size = Marshal.SizeOf(inputBuffer[0]) * inputBuffer.Length;
        IntPtr pnt = Marshal.AllocHGlobal(size);

        try
        {
            // Copy the array to unmanaged memory.
            Marshal.Copy(inputBuffer, 0, pnt, inputBuffer.Length);
            success = hapticDevice_nextSample(idFrame, pnt, inputBuffer.Length);
        }
        finally
        {
            // Free the unmanaged memory.
            Marshal.FreeHGlobal(pnt);
        }
        return success;
    }

    public uint getBufferSize()
    {
        return hapticDevice_getBufferSize();
    }

    public uint getMistData()
    {
        return hapticDevice_getMissingDataCount();
    }

    public bool close()
    {
        initialized = false;
        return hapticDevice_close();
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

    void OnApplicationQuit()
    {
        if (initialized)
        {
            hapticDevice_close();
        }
    }
}
