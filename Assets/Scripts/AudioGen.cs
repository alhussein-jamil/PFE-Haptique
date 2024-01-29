using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;


public class AudioGen : AbstractSamplesGenerator
{
    public enum e_Channel
    {
        LEFT, RIGHT, COMBINED
    }

    public enum e_SampleRate
    {
        SOURCE_FREQUENCY, DOWN_SAMPLE // RESAMPLE
    }

    public HapticManager.e_base_frequency deviceBaseFrequency = HapticManager.e_base_frequency.VIB_DEVICE_FREQUENCY;
    public e_Channel sampleChannel = e_Channel.COMBINED;
    public e_SampleRate sampleRate = e_SampleRate.DOWN_SAMPLE;

    private AudioClip audioFile;
    private float[] audioSamples;
    private int idx_sig = 0;
    private int audioChannels;
    private int audioSize;

    // Nom du fichier audio à lire
    public string audioFileName;

    
    private void Awake()
    {
        LoadAudioFile("signal120_0_1");

        if (!initialized) initSamples(); //Audio source should be initilized in main thread
    }

    //Méthode pour charger le fichier audio en fonction du nom spécifié
    public void LoadAudioFile(string audioName)
    {

        audioFileName = audioName;

        // Charge le fichier audio en fonction du nom spécifié
        audioFile = Resources.Load<AudioClip>(audioFileName);
        
        initSamples();

    }

    public override float Frequency
    {
        get { return HapticManager.Instance != null ? HapticManager.Instance.getFrequency(deviceBaseFrequency) : 4000f; }
    }

    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        if (!initialized)
        {
            Debug.LogWarning("Audio Source not initialized");
            sampleEnded = true;
            return new float[size];
        }

        sampleEnded = false;
        var samples = new float[size];

        for (int i = 0; i < size; i++)
        {
            float currentValue = 0;

            if (sampleChannel == e_Channel.COMBINED && audioChannels > 1)
            {
                for (int j = 0; j < audioChannels; j++)
                {
                    currentValue += audioSamples[idx_sig * audioChannels + j];
                }
                currentValue = currentValue / audioChannels;
                samples[i] = currentValue;
            }
            else
            {
                currentValue = audioSamples[idx_sig * audioChannels + (sampleChannel == e_Channel.LEFT || audioChannels == 1 ? 0 : 1)];
                samples[i] = currentValue;
            }

            idx_sig = ++idx_sig % audioSize;
            sampleEnded = idx_sig == 0;

            if (sampleEnded && !loop)
            {
                for (int j = i + 1; j < size; j++)
                {
                    samples[j] = 0f;
                }
                break;
            }
        }

        return samples;
    }

   public override void initSamples()
    {
        audioChannels = audioFile.channels;
        int audioSizeSource = audioFile.samples;
        float downsample = sampleRate == e_SampleRate.DOWN_SAMPLE ? audioFile.frequency / Frequency : 1;

        float[] fullSample = new float[audioSizeSource * audioChannels];
        audioFile.GetData(fullSample, 0);
        audioSize = Mathf.FloorToInt(audioSizeSource / downsample);

        audioSamples = new float[audioSize * audioChannels];
        for (int i = 0; i < audioSize; i++)
        {
            for (int j = 0; j < audioChannels; j++)
            {
                audioSamples[i * audioChannels + j] = fullSample[Mathf.FloorToInt(i * downsample) * audioChannels + j];
            }
        }

        idx_sig = 0;
        initialized = true;
    }
}
