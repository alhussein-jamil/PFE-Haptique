using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Utils
{
    public static T getEnumFromString<T>(string value, T defaultValue)
    {
        foreach (int i in Enum.GetValues(typeof(T)))
        {
            if( Enum.GetName(typeof(T), i).ToLower() ==  value.ToLower())
			{
				return (T)Enum.ToObject(typeof(T), i); ;
            }
        }
		return defaultValue;
    }

    public static T[] SubArray<T>(T[] data, int index, int length)
	{
		T[] result = new T[length];
		System.Array.Copy(data, index, result, 0, length);
		return result;
	}

	//Concert a int to a array of byte (BigEndian)
	public static byte[] ConvertIntToBytes(int value)
	{
		byte[] leb = System.BitConverter.GetBytes(value);
		if (System.BitConverter.IsLittleEndian)
			System.Array.Reverse(leb);
		return leb;
	}

    //Same than ConvertIntToBytes but return a array of byte defined size
    public static byte[] ConvertIntToBytes(int value, int size)
    {
        byte[] leb = System.BitConverter.GetBytes(value);
        if (System.BitConverter.IsLittleEndian)
            System.Array.Reverse(leb);
        return Utils.SubArray(leb, leb.Length - size, size);
    }

    public static byte ConvertIntToByte(int value)
	{
		try
		{
			return System.Convert.ToByte(value);
		}
		catch (System.OverflowException)
		{
			System.Console.WriteLine("The {0} value {1} is outside the range of the Byte type.", value.GetType().Name, value);
			return 0;
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

    public static string ConvertByteArrayToString(byte[] bytes)
    {
        var sb = new StringBuilder("{ ");
        foreach (var b in bytes)
        {
            sb.Append(b + ", ");
        }
        sb.Append("}");
       return sb.ToString();
    }

    public static int floatToPWM(float value, int maxPWM, bool center = true)
	{
        if (center)
            return (int)(Mathf.Clamp01((value + 1) / 2f) * maxPWM);
        else
            return (int)(Mathf.Clamp01(value) * maxPWM);
    }

    //Convert a float value of amplitude [-1,1] (or [0-1] if not center) to a byte PWM value [0,MaxPWM]
    public static byte ConvertToPWM(float value, int maxPWM, bool center = true) 
	{
		if (center)
			return Utils.ConvertIntToByte((int)(Mathf.Clamp01((value + 1) / 2f) * maxPWM));
		else
			return Utils.ConvertIntToByte((int)(Mathf.Clamp01(value) * maxPWM));

	}

    //Convert a float value of amplitude [-1,1] (or [0-1] if not center) to a PWM value [0,MaxPWM] in array of byte 
    public static byte[] ConvertToLargePWM(float value, int maxPWM, bool center = true, int sizeByte = 2)
    {
        if (center)
            return Utils.ConvertIntToBytes((int)(Mathf.Clamp01((value + 1) / 2f) * maxPWM), sizeByte);
        else
            return Utils.ConvertIntToBytes((int)(Mathf.Clamp01(value) * maxPWM), sizeByte);

    }

    //Convert an array of float amplitude value [-1,1] to a byte PWM value [0,MaxPWM]
    public static byte[] ConvertToPWM(float[] values, int maxPWM)
	{
		byte[] res = new byte[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
			res[i] = ConvertToPWM(values[i], maxPWM);
				
		}
		return res;
	}

	public static IEnumerable<float> LinSpace(float start, float stop, int num, bool endpoint = true)
	{
		var result = new List<float>();
		if (num <= 0)
		{
			return result;
		}

		if (endpoint)
		{
			if (num == 1)
			{
				return new List<float>() { start };
			}

			float step = (stop - start) / ((float)num - 1.0f);
			result = System.Linq.Enumerable.Range(0, num).Select(v => (v * step) + start).ToList();
		}
		else
		{
			float step = (stop - start) / (float)num;
			result = System.Linq.Enumerable.Range(0, num).Select(v => (v * step) + start).ToList();
		}

		return result;
	}

    public static int ToNearestPowerOfTwo(int x)
    {
        return (int)Mathf.Pow(2, Mathf.Round(Mathf.Log(x) / Mathf.Log(2)));
    }
}
