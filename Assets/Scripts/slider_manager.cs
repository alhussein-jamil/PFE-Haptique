using Unity.Robotics.UrdfImporter;
using UnityEngine;
using UnityEngine.UI;
using System;
public class slider_manager : MonoBehaviour
{
    public GameObject sliderContainer;
    public Slider[] positionSliders;
    private ArticulationBody[] articulationChain;
    public float[] encoderValues;
    
    public bool[] activatedsliders;
    public float[] sliderValues;
    void Start()
    {
        positionSliders = sliderContainer.GetComponentsInChildren<Slider>();
        articulationChain = this.GetComponentsInChildren<ArticulationBody>();
        encoderValues = new float[positionSliders.Length];
        sliderValues = new float[positionSliders.Length];
        // add listeners to sliders value changes 
        for (int idx = 0; idx < positionSliders.Length; idx++)
        {
            ArticulationBody joint = articulationChain[idx+1];
            positionSliders[idx].minValue = joint.xDrive.lowerLimit;
            positionSliders[idx].maxValue = joint.xDrive.upperLimit;
        }
    
    }   

    void Update()
    {
        // set limits of sliders to the limits of the joints 
        for (int idx = 0; idx < positionSliders.Length; idx++)
        {
            ArticulationBody joint = articulationChain[idx+1];
            
            joint.SetDriveTarget(axis: ArticulationDriveAxis.X, value: positionSliders[idx].value);

            encoderValues[idx] = joint.jointPosition[0] / (float)Math.PI * 180;

            sliderValues[idx] = positionSliders[idx].value;
        }

    }
}