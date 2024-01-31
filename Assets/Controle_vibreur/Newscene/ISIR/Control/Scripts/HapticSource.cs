using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

public class HapticSource : MonoBehaviour
{
    public enum e_operator
    {
        SUM, PRODUCT, AVERAGE
    }

    [Tooltip("Volume of the signal")]
    [Range(0, 1)]
    [SerializeField]
    private float volume = 1;
    [Tooltip("Limit of the signal")]
    [Range(0, 1)]
    [SerializeField]
    private float maxVolume = 1;
    public e_operator mixingOperator;
    public List<PlayModeParameter> playmodeParameters;

    private float[] lastData = { };
    private ulong lastFrameID = 0;

    public float Volume { get => volume; set => volume = value; }
    public float MaxVolume { get => maxVolume; set => maxVolume = value; }
    public AbstractSamplesGenerator getHapticClip(int id = 0) {
        if (id < playmodeParameters.Count && id >= 0)
        { return playmodeParameters[id].hapticClip; }
        else return null;
    }

    public PlayModeParameter getHapticClipParameters(int id = 0)
    {
        if (id < playmodeParameters.Count && id >= 0)
        { return playmodeParameters[id]; }
        else return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (var pp in playmodeParameters)
        {
            if(pp.hapticClipPrefab != null)
            {
                pp.hapticClip = pp.hapticClipPrefab.GetComponent<AbstractSamplesGenerator>();
                pp.state = e_stateSource.STOP;
                if (pp.startOnAwake)
                {
                    start();
                }
            }
        }
        if(playmodeParameters.Count <= 0)
        {
            Debug.LogWarning("HapticSource " + gameObject.name + " with empty prefab, will be ignore");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.updateTimer();
        }
    }

    public void start()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.start();
        }
    }
    public void stop()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.stop();
        }
    }

    public void pause()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.pause();
        }
    }

    public void reset()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.reset();
        }
    }

    private void initSamples()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.hapticClip.initSamples();
        }
    }

    public void togglePause()
    {
        foreach (var pp in playmodeParameters)
        {
            pp.togglePause();
        }
    }

    private float[] applyVolume(float[] data, float volume, float maxVol = 1)
    {    
        return data.Select(x => Mathf.Clamp(x * volume, -1 * maxVol, maxVol)).ToArray();
    }


    public float[] getSamples(ulong frameID, int size)
    {
        if (lastData.Length < size)
        {
            lastData = lastData.Concat(Enumerable.Repeat(0f, size - lastData.Length).ToArray()).ToArray();
        }
        if (frameID != lastFrameID)
        {
            bool ended = false;
            float[] data;
            lastData = Enumerable.Repeat(0f, size).ToArray();
            int countChannel = 0;
            foreach (var pp in playmodeParameters)
            {
                if(pp.state == e_stateSource.STARTED)
                {
                    data = pp.hapticClip.getNextSamples(size, out ended, pp.playModeType != PlayModeParameter.e_playmode_type.ONCE);
                    
                    data = applyVolume(data, pp.volume);

                    for (int i = 0; i < size; i++)
                    {
                        if (mixingOperator == e_operator.PRODUCT)
                        {
                            lastData[i] = lastData[i] * data[i];
                        }
                        else
                        {
                            lastData[i] = lastData[i] + data[i];
                        }
                    }
                    countChannel++;
                }
                if (pp.playModeType == PlayModeParameter.e_playmode_type.ONCE && ended)
                {
                    pp.state = e_stateSource.STOP;
                }
            }
            if (mixingOperator == e_operator.AVERAGE && countChannel > 1)
            {
                for (int i = 0; i < size; i++)
                {
                    lastData[i] = lastData[i] / countChannel;
                }
            }
            lastFrameID = frameID;
        }
        
        return applyVolume(lastData, Volume, MaxVolume);
    }

}

public enum e_stateSource
{
    UNSET = 0, STARTED, STOP, PAUSE
}

//Parameter of the play mode
[System.Serializable]
public class PlayModeParameter
{
    [SerializeField]
    public AbstractSamplesGenerator hapticClipPrefab;
    public AbstractSamplesGenerator hapticClip = null;
    public enum e_playmode_type
    {
        LOOP = 0, ONCE, DURATION
    }
    [Tooltip("Source will be ready to play on start")]
    public bool startOnAwake = false;
    [Tooltip("Play mode")]
    public e_playmode_type playModeType = e_playmode_type.LOOP;
    [Tooltip("Duration of the source, only available of Duration playmode")]
    public float duration = 1;
    [Tooltip("Volume of the signal")]
    [Range(0, 1)]
    [SerializeField]
    public float volume = 1;
    public float currentCountdown = 0;
    public e_stateSource state = e_stateSource.UNSET;

    public AbstractSamplesGenerator getHapticClip() { return hapticClip; }

    public void updateTimer()
    {
        if (state == e_stateSource.STARTED && playModeType == PlayModeParameter.e_playmode_type.DURATION)
        {
            currentCountdown -= Time.deltaTime;
            if (currentCountdown <= 0)
            {
                state = e_stateSource.STOP;
            }
        }
    }

    public void start()
    {
        if (state == e_stateSource.STOP)
        {
            if (playModeType == PlayModeParameter.e_playmode_type.DURATION)
            {
                currentCountdown = Mathf.Max(0, duration);
            }
            state = e_stateSource.STARTED;
            initSamples();
        }
        else if (state == e_stateSource.PAUSE)
        {
            state = e_stateSource.STARTED;
        }
    }
    public void stop()
    {
        state = e_stateSource.STOP;
    }

    public void pause()
    {
        if (state == e_stateSource.STARTED)
        {
            state = e_stateSource.PAUSE;
        }
    }

    public void reset()
    {
        if (state != e_stateSource.UNSET)
        {
            initSamples();
        }
    }

    private void initSamples()
    {
       hapticClip.initSamples();
    }

    public void togglePause()
    {
        if (state == e_stateSource.PAUSE)
        {
            state = e_stateSource.STARTED;
        }
        else if (state == e_stateSource.STARTED)
        {
            state = e_stateSource.PAUSE;
        }
    }
}


#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(PlayModeParameter))]
public class PlayModeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var playmodeType = property.FindPropertyRelative("playModeType");

        int numberOfElement = playmodeType.enumValueIndex == 2 ? 6 : 5;
        // The 6 comes from extra spacing between the fields (2px each)
        return EditorGUIUtility.singleLineHeight * numberOfElement + (numberOfElement-1 * 2);
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Play Mode Parameters : "));

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 2;
        var playModeTypeProp = property.FindPropertyRelative("playModeType");
        int addSpace = playModeTypeProp.enumValueIndex == (int)PlayModeParameter.e_playmode_type.DURATION ? 20 : 0;
        // Calculate rects
        var prefabRect = new Rect(0, position.y+18, position.width+ position.x, 16);
        var startOnAwakeRect = new Rect(0, position.y + 34, position.width+ position.x, 16);
        var playModeRect = new Rect(0, position.y + 54 , position.width + position.x, 16);
        var durationRect = new Rect(0, position.y + 74 , position.width + position.x, 16);
        var volumeRect = new Rect(0, position.y + 74+ addSpace, position.width + position.x, 16);
        // Draw fields - pass GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(prefabRect, property.FindPropertyRelative("hapticClipPrefab"));
        EditorGUI.PropertyField(startOnAwakeRect, property.FindPropertyRelative("startOnAwake"));
        EditorGUI.PropertyField(playModeRect, playModeTypeProp);

        if (playModeTypeProp.enumValueIndex == (int)PlayModeParameter.e_playmode_type.DURATION)
        {
            var durationProp = property.FindPropertyRelative("duration");
            //durationProp.floatValue = EditorGUI.FloatField(durationRect, "Duration", durationProp.floatValue);
            //EditorGUI.PropertyField(durationRect, durationProp);
            EditorGUI.Slider(durationRect, durationProp, 0, 100, "Duration (s)");
        }

        var volumeProp = property.FindPropertyRelative("volume");
        EditorGUI.Slider(volumeRect, volumeProp, 0, 1, "Volume");

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
#endif
