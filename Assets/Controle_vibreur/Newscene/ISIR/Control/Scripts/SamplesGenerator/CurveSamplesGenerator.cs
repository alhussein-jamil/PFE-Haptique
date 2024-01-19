using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CurveSamplesGenerator : AbstractSamplesGenerator
{
    public HapticManager.e_base_frequency deviceBaseFrequency = HapticManager.e_base_frequency.VIB_DEVICE_FREQUENCY;

    public AnimationCurve samplesCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Take data only in the curve time range or continu when go over time range")]
    public bool DataOnlyOnCurveRange = false;
    private float currentTime = 0;

    public override float Frequency { get => HapticManager.Instance != null ? HapticManager.Instance.getFrequency(deviceBaseFrequency) : 5000f; }

    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        if (!initialized) initSamples();
        float deltaTime = 1 / Frequency;
        float lastKeyTime = samplesCurve.keys[samplesCurve.length - 1].time;
        sampleEnded = false;
        List<float> samples = new List<float>();
        for (int i = 0; i < size; i++)
        { 
            samples.Add(samplesCurve.Evaluate(currentTime));
            currentTime += deltaTime;
            sampleEnded = currentTime > lastKeyTime;
            if (DataOnlyOnCurveRange && sampleEnded) { currentTime = currentTime % lastKeyTime; }
            if (sampleEnded && !loop)
            {
                samples.AddRange(Enumerable.Repeat(0f, size - (i + 1))); //If we don't when to loop we finish the sample with 0f
                break;
            }
        }

        return samples.ToArray<float>();
    }

    public override void initSamples()
    {
        currentTime = 0;
    }

   
}
