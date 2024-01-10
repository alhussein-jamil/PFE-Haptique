using UnityEngine;
using UnityEngine.UI;
using System;

namespace Franka
{
    public class slider_manager : MonoBehaviour
    {        
        public float[] sliderValues;
        public GameObject sliderContainer;
        public Slider[] positionSliders;
        public float[] encoderValues;

        private ArticulationBody[] articulationChain;
        private RedisConnection redisConnection;
        private string previousMessage;
        private command controller;

        void Start()
        {
            redisConnection = this.GetComponent<RedisConnection>();
            positionSliders = sliderContainer.GetComponentsInChildren<Slider>();
            controller = this.GetComponent<command>();
            articulationChain = controller.articulationChain;
            encoderValues = new float[positionSliders.Length];
            sliderValues = new float[positionSliders.Length];
            previousMessage = "";
            
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
            string message = "";
            // set limits of sliders to the limits of the joints 
            for (int idx = 0; idx < positionSliders.Length; idx++)
            {
                ArticulationBody joint = articulationChain[idx+1];
                
                // set joints by publishing the values to redis
                message += positionSliders[idx].value.ToString() + ";";

                encoderValues[idx] = joint.jointPosition[0] / (float)Math.PI * 180;

                sliderValues[idx] = positionSliders[idx].value;
            }

            // publish the values to redis only if they have changed
            if (message != previousMessage)
            {
                redisConnection.publisher.Publish(redisConnection.sim_robot_channel, message);
                previousMessage = message;
            }
        }
    }
}