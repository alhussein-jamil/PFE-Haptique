using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConstSamplesGenerator : AbstractSamplesGenerator
{
    public float value = 1;

    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        sampleEnded = false;
        return Enumerable.Repeat(value, size).ToArray();
    }

    public override void initSamples()
    {
        initialized = true;
    }

}
