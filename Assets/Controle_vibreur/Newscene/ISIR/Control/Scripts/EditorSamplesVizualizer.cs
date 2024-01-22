using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorSamplesVizualizer : MonoBehaviour
{
    [Header("Display a HapticClip, go to 'Menu/Generate Curve' of the script to generate the curve")]
    public AbstractSamplesGenerator hapticClipPrefab;
    private AbstractSamplesGenerator hapticClip;
    [Range(0, 10)]
    public float duration = 1;
    [Range(1,10000)]
    public float frequency = 10000f;
    [Tooltip("Allow to reduce the number of key add to create the curves")]
    [Range(0, 1)]
    public float precision = 0.1f;
    public bool loop = true;
    [Header("Output Curve :")]
    public AnimationCurve outputCurve;

    [ContextMenu("Clear Curve")]
    void clearCurve()
    {
        for (int i = outputCurve.length - 1; i >= 0; i--)
        {
            outputCurve.RemoveKey(i);
        }
    }

    [ContextMenu("Generate Curve")]
    void GetSourceFromCurrentGameObject()
    {
        if(HapticManager.Instance == null)
        {
            Debug.LogWarning("HapticManager not found, default frequency 10000Hz will be use");
        }
        if (hapticClipPrefab != null)
        {
            hapticClip = Instantiate(hapticClipPrefab).GetComponent<AbstractSamplesGenerator>();
            int sizeCurve = Mathf.CeilToInt(duration * frequency);
            float deltaTime = 1 / frequency;
            int step = (int) (1f / precision);
            bool end = false;
            float[] samples= hapticClip.getNextSamples(Mathf.CeilToInt(sizeCurve), out end, loop);
            clearCurve();

            for (int i = 0; i < sizeCurve*precision; i++)
            {
                int index = i * step;
                if (index == 0 || index + 1 == sizeCurve || samples[index - 1] != samples[index] || samples[index] != samples[index + 1]) 
                {
                    outputCurve.AddKey(new Keyframe(index * deltaTime, samples[index], 0,0,0,0));
                }
            }
            Debug.Log("Clip Generated (" + outputCurve.length + " keys)");
        }

        if (hapticClip == null)
        { Debug.LogWarning("No Haptic Clip found"); }
        else
        {
            DestroyImmediate(hapticClip.gameObject);
        }
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
