using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Franka
{
public class Oscillation : MonoBehaviour
{
        private Command controller;
        public float amplitude = 100.0f;
        public float frequency = 0.5f;
        public float[] encoderValues;

        void Start()
        {
            controller = GetComponent<Command>();
                        encoderValues = controller.encoderValues;   



        }

    // Update is called once per frame
    void Update()
    {


        for (int idx = 0; idx < encoderValues.Length; idx++)
        {
            encoderValues[idx] = amplitude * Mathf.Sin(frequency * Time.time);
        }

        
    }
}
}