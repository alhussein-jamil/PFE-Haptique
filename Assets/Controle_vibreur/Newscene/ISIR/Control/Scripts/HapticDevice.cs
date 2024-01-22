using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class HapticDevice : MonoBehaviour
{
    [SerializeField]
    protected HapticSource sourceSignal;
    [Tooltip("Device volume factor")]
    [Range(0, 1)]
    [SerializeField]
    public float deviceVolume = 1;
    [SerializeField]
    private bool active = true;

    public float Volume { get => deviceVolume; private set => deviceVolume = value; }
    public bool Active { get => active; set => active = value; }
    public void setSource(HapticSource si)
    {
        sourceSignal = si;
    }

    public float[] getSamples(ulong frameID, int size)
    {
        float[] d = Enumerable.Repeat(0f, size).ToArray();
        if (Active && sourceSignal != null)
        {
            d = sourceSignal.getSamples(frameID, size);
            d = applyVolume(d, Volume);
        }
        return d;
    }

    private float[] applyVolume(float[] data, float volume, float maxVol = 1)
    {
        return data.Select(x => Mathf.Clamp(x * volume, -1 * maxVol, maxVol)).ToArray();
    }

}
