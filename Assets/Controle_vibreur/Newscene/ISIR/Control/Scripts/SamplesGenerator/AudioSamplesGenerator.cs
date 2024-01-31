using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class AudioSamplesGenerator : AbstractSamplesGenerator
{

    public enum e_Channel
    {
        LEFT, RIGHT, COMBINED
    }
    public enum e_SampleRate
    {
        SOURCE_FREQUENCY, DOWN_SAMPLE //RESAMPLE
    }

    public HapticManager.e_base_frequency deviceBaseFrequency = HapticManager.e_base_frequency.VIB_DEVICE_FREQUENCY;
    public AudioClip audioFile;
    public e_Channel sampleChannel = e_Channel.COMBINED;
    public e_SampleRate sampleRate = e_SampleRate.DOWN_SAMPLE;

    int idx_sig = 0;
    int audioChannels = 0;
    int audioFrequency = 1;
    int audioSize = 0;
    float[] audioSamples;
    public Brush_Movement brush_script;
    public float brush_speed;

    public int trial_numb = 0; //actual trial number
    [SerializeField] private int actuator_numb;
    [SerializeField] private string fileName;

    private string audioFileListFileName = "audio_file_list"; // Name of the audio file list text file (without extension)

    private Dictionary<string, string> audioFileMap = new Dictionary<string, string>();

    private int trial_previous = 0;
    private TextAsset csvText;


    // when button NEXT clicked this function activated
    public void NextTrial()
    {
        trial_numb += 1;

        if (trial_numb > trial_previous && csvText != null)
        {
            LoadAudioFileNameFromCSV(trial_numb, actuator_numb);
            
            initSamples();
        }
    }


    // Function to load audio file names from a CSV based on trial and bowl indices
    public void LoadAudioFileNameFromCSV(int targetTrial, int targetObject)
    {
        // Assuming the CSV file is named "GeneratedData.csv"
        csvText = Resources.Load<TextAsset>("GeneratedData");

        // Split CSV file into lines
        string[] lines = csvText.text.Split('\n');
        int rowCount = lines.Length;
        int columnCount = lines[0].Split(',').Length;

        // Validate target trial and object indices
        if (targetTrial >= 1 && targetTrial <= rowCount && targetObject >= 1 && targetObject <= columnCount)
        {
            string[] headers = lines[0].Split(',');

            // Extract trial name and index
            string trialName = headers[targetObject].Trim();
            int trialIndex = -1;

            // Find the row index corresponding to the target trial
            for (int i = 1; i < rowCount; i++)
            {
                string[] rowData = lines[i].Split(',');

                if (rowData[0].Trim() == "Trials " + targetTrial)
                {
                    trialIndex = i;
                    break;
                }
            }

            // Process the trial data
            if (trialIndex != -1 && trialIndex <= rowCount)
            {
                string[] trialData = lines[trialIndex].Split(',');
                string fileIdentifier;


                if (actuator_numb == 1) //first actuator
                {
                    fileIdentifier = trialData[2].Trim();
                    
                }
                else if (actuator_numb == 2) //second
                {
                    fileIdentifier = trialData[3].Trim();
                }
                else {
                    fileIdentifier = trialData[4].Trim(); //third 
                }

                if (trial_numb >= 1)
                {
                    brush_speed = float.Parse(trialData[1].Trim()); //reading the brush speed
                    brush_script.UpdateSpeed_Brush(brush_speed);
                }


                if (audioFileMap.TryGetValue(fileIdentifier, out string audioFileName))
                {

                    // Load audio file based on file identifier
                    audioFile = Resources.Load<AudioClip>(audioFileName);
                    if (audioFile != null)
                    {
                        Debug.LogWarning("Audio clip found for file name!! " + audioFileName);
                    }
                    else
                    {
                        Debug.LogWarning("Audio clip not found for file name: " + audioFileName);
                    }
                }
                else
                {
                    Debug.LogWarning("Audio file name not found in the list: " + fileName);
                }
            }
            else
            {
                Debug.LogWarning("END OF THE EXPEIRMENT + Invalid trial index.");
            }
        }
        else
        {
            Debug.LogWarning("Invalid trial or object index.");
        }
    }

    // Function to load audio file name mappings from a text asset and create the dictionary audioFileMap with indexes and audio
    /* txt file sctructure -> [index of the audio],[name of the audio file without ".wav"] */
    private void LoadAudioFileMap()
    {
        TextAsset audioFileListText = Resources.Load<TextAsset>(audioFileListFileName);
        if (audioFileListText != null)
        {
            string[] lines = audioFileListText.text.Split('\n');
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string fileIdentifier = parts[0].Trim();
                    string audioFileName = parts[1].Trim();
                    audioFileMap[fileIdentifier] = audioFileName;
                }
            }
        }
        else
        {
            Debug.LogError("Audio file list text asset not found: " + audioFileListFileName);
        }
    }


    private void Awake()
    {
        LoadAudioFileNameFromCSV(trial_numb, actuator_numb);
        LoadAudioFileMap();

        if (!initialized) initSamples(); //Audio source should be initilized in main thread
    }



    public override float Frequency { get => HapticManager.Instance != null ? HapticManager.Instance.getFrequency(deviceBaseFrequency) : 4000f; }

    public override float[] getNextSamples(int size, out bool sampleEnded, bool loop = true)
    {
        if (!initialized)
        {
            Debug.LogWarning("Audio Source not initilized");
            sampleEnded = true;
            return Enumerable.Repeat(0f, size).ToArray<float>();
        }
        sampleEnded = false;
        List<float> samples = new List<float>();
        //int idx = sampleRate == e_SampleRate.DOWN_SAMPLE ? Mathf.FloorToInt(idx_sig * audioFrequency / Frequency) : idx_sig;
        //float[]  subAudioSamples = Utils.SubArray<float>(audioSamples, idx, size * audioChannels);
        //audioFile.GetData(audioSamples, idx);

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
                samples.Add(currentValue);
            }
            else
            {
                currentValue = audioSamples[idx_sig * audioChannels + (sampleChannel == e_Channel.LEFT || audioChannels == 1 ? 0 : 1)];
            }
            samples.Add(currentValue);


            idx_sig = ++idx_sig % audioSize;
            sampleEnded = idx_sig == 0;
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
        //float[] samples = new float[audioFile.samples * audioFile.channels];
        if (audioFile)
        {
            audioChannels = audioFile.channels;
            audioFrequency = audioFile.frequency;
            int audioSizeSource = audioFile.samples;
            float downsample = sampleRate == e_SampleRate.DOWN_SAMPLE ? audioFrequency / Frequency : 1;

            float[] fullSample = new float[audioSizeSource * audioChannels];
            audioFile.GetData(fullSample, 0);
            audioSize = Mathf.FloorToInt(audioSizeSource / downsample);

            audioSamples = new float[audioSize * audioChannels];
            for (int i = 0; i < audioSize; i++)
            {
                for (int j = 0; j < audioChannels; j++) //TODO faire le mixage a ce moment
                {
                    audioSamples[i * audioChannels + j] = fullSample[Mathf.FloorToInt(i * downsample) * audioChannels + j];
                }
            }
            //Debug.Log(audioChannels + "  " + audioFrequency + "  " + audioSize);
            idx_sig = 0;
            initialized = true;
        }
    }

}