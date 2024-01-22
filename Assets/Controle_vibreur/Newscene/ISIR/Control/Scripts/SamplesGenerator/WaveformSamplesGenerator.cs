using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class WaveformSamplesGenerator : AbstractSamplesGenerator
{
    public enum e_waveform
    {
        SIN = 0, SQUARE, TRIANGLE
    }

    public HapticManager.e_base_frequency deviceBaseFrequency = HapticManager.e_base_frequency.VIB_DEVICE_FREQUENCY;
    public e_waveform waveform = e_waveform.SIN;
    public float waveFrequency = 200;
    int idx_sig = 0;
    float[] signal;

    public override float Frequency { get => HapticManager.Instance != null ? HapticManager.Instance.getFrequency(deviceBaseFrequency) : 5000f; }


    private float[] generateSin()
    {
        List<float> wave = Utils.LinSpace(0, 1 / waveFrequency - 1 / Frequency, (int)(Frequency / waveFrequency)).Select(t => Mathf.Sin(2f * Mathf.PI * waveFrequency * t)).ToList();
        return wave.ToArray();
    }

    private float[] generateSquare()
    {
        List<float> wave = Utils.LinSpace(0, 1 / waveFrequency - 1 / Frequency, (int)(Frequency / waveFrequency)).Select(t => (t < (1 / waveFrequency - 1 / Frequency) / 2 ? 1f : -1f)).ToList();
        return wave.ToArray();
    }

    private float[] generateTriangle()
    {
        float A = 2;
        float P = (1 / waveFrequency - 1 / Frequency) / 2;
        List<float> wave = Utils.LinSpace(0, 1 / waveFrequency - 1 / Frequency, (int)(Frequency / waveFrequency)).Select(t => ((A / P) * (P - Mathf.Abs(t % (2 * P) - P)))-1).ToList();
        return wave.ToArray();
    }

    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        if(!initialized) { initSamples(); }
        sampleEnded = false;
        List<float> samples = new List<float>();
        for (int i = 0; i < size; i++)
        {
            samples.Add(signal[idx_sig]);
            idx_sig = ++idx_sig % signal.Length;
            sampleEnded = idx_sig == 0;
            if(sampleEnded && !loop)
            {
                samples.AddRange(Enumerable.Repeat(0f, size-(i+1))); //If we don't when to loop we finish the sample with 0f
                break;
            }
        }

        return samples.ToArray<float>();
    }

    
    public override void initSamples()
    {
        idx_sig = 0;
        switch (waveform)
        {
            case e_waveform.SQUARE:
                signal = generateSquare();
                break;
            case e_waveform.TRIANGLE:
                signal = generateTriangle();
                break;
            default:
                signal = generateSin();
                break;
        }
        initialized = true;

        Debug.Log("SIGNAL");
        string t = "";
        foreach (var i in signal)
        {
            t += i.ToString() + ";";
        }
        Debug.Log(t);

    }


}
