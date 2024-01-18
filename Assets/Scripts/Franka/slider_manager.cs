using UnityEngine;
using UnityEngine.UI;
using System;

namespace Franka
{
    public class SliderManager : MonoBehaviour
    {
        public float[] sliderValues;
        public GameObject sliderContainer;
        public Slider[] positionSliders;
        public float[] encoderValues;

        private ArticulationBody[] articulationChain;
        private RedisConnection redisConnection;
        private string previousMessage;
        private Command controller;

        private bool doneInit = false;

        void Start()
        {
            redisConnection = GetComponent<RedisConnection>();
            positionSliders = sliderContainer.GetComponentsInChildren<Slider>();
            controller = GetComponent<Command>();
            encoderValues = new float[positionSliders.Length];
            sliderValues = new float[positionSliders.Length];
            previousMessage = "";
        }

        void PostInit()
        {
            articulationChain = controller.articulationChain;

            for (int idx = 0; idx < positionSliders.Length; idx++)
            {
                ArticulationBody joint = articulationChain[idx + 1];

                positionSliders[idx].minValue = joint.xDrive.lowerLimit;
                positionSliders[idx].maxValue = joint.xDrive.upperLimit;
            }
            doneInit = true;
        }

        void Update()
        {
            if (!doneInit && controller.subscriptionDone)
                PostInit();

            string message = "";

            // Set limits of sliders to the limits of the joints 
            for (int idx = 0; idx < positionSliders.Length; idx++)
            {
                if (articulationChain == null)
                    continue;

                ArticulationBody joint = articulationChain[idx + 1];

                // Set joints by publishing the values to redis
                message += positionSliders[idx].value.ToString() + ";";

                encoderValues[idx] = joint.jointPosition[0];

                sliderValues[idx] = positionSliders[idx].value;
            }

            // Publish the values to redis only if they have changed
            if (message != previousMessage)
            {
                redisConnection.publisher.Publish(redisConnection.simRobotChannel, message);
                previousMessage = message;
            }
        }
    }
}
