using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MixerSamplesGenerator : AbstractSamplesGenerator
{
    public enum e_operator
    {
        MUL,
        ADD,
        AVR
    }

    public AbstractSamplesGenerator sampleGen1;
    public AbstractSamplesGenerator sampleGen2;
    public e_operator mixOperator = e_operator.AVR;

    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        bool sampleEndedTemp = false;
        float[] samples = new float[size];
        float[] samples1 = sampleGen1.getNextSamples(size, out sampleEndedTemp, loop);
        sampleEnded = sampleEndedTemp;
        float[] samples2 = sampleGen2.getNextSamples(size, out sampleEndedTemp, loop);
        sampleEnded = sampleEnded || sampleEndedTemp;
        for (int i = 0; i < size; i++)
        {
            if(mixOperator == e_operator.MUL)
            {
                samples[i] = samples1[i] * samples2[i]; //TODO attention valeur en pwm non centré a refaire
            }else if(mixOperator == e_operator.ADD)
            {
                samples[i] = samples1[i] + samples2[i];
            }
            else
            {
                samples[i] =  (samples1[i] + samples2[i]) / 2; //TODO attention valeur en pwm non centré a refaire
            }
        }
            //verifier que dans le range
        return samples;
    }


    public override void initSamples()
    {
        initialized = true;
    }


}
