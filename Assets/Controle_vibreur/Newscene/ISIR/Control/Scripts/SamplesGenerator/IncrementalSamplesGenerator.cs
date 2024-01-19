using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncrementalSamplesGenerator : AbstractSamplesGenerator
{

    float iter = -1;  
    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        sampleEnded = false;
        float[] d = new float[size];
        for (int i = 0; i < size; i++)
        {
            d[i] = iter;
            iter += 1f/186f;
            if(iter > 1)
            { iter = -1; }
        }
        return d;
    }

    public override void initSamples()
    {
        initialized = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
