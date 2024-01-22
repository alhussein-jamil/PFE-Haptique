using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class AbstractSamplesGenerator : MonoBehaviour
{
    protected bool initialized = false;
    //Base frequency depend on the device
    public virtual float Frequency { get => HapticManager.Instance != null ?  HapticManager.Instance.frequencyVib : 5000f;  }

    public abstract void initSamples();
    public abstract float[] getNextSamples(int size, out bool sampleEnded, bool loop = true);

}