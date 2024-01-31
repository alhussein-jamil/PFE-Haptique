using System;
using System.Collections.Generic;
using StackExchange.Redis;
using UnityEngine;
using UnityEngine.UI;

namespace Franka
{
    public class SliderManager : MonoBehaviour
    {
        public GameObject sliderContainer;
        public Slider[] positionSliders;
        public double[] encoderValues;
        public float[] sliderValues;
        private ArticulationBody[] articulationChain;
        public RedisConnection redisConnection;

        private Command controller;

        private bool doneInit = false;

        void Start()
        {
            redisConnection = GetComponent<RedisConnection>();
            positionSliders = sliderContainer.GetComponentsInChildren<Slider>();
            controller = GetComponent<Command>();
            encoderValues = new double[positionSliders.Length];
            sliderValues = new float[positionSliders.Length];

            InvokeRepeating("Publish", 0f, 0.001f);
        }

        void PostInit()
        {

            for (int idx = 0; idx < positionSliders.Length; idx++)
            {
                ArticulationBody joint = articulationChain[idx + 1];

                positionSliders[idx].minValue = joint.xDrive.lowerLimit / 180 * (float)Math.PI;
                positionSliders[idx].maxValue = joint.xDrive.upperLimit / 180 * (float)Math.PI;
            }

            doneInit = true;
        }

        void Publish()
        {
            if (!redisConnection.doneInit)
                return;

            for (int idx = 0; idx < encoderValues.Length; idx++)
            {
                encoderValues[idx] = positionSliders[idx].value;
            }
            byte[] bytes = RedisConnection.CoordsToLine(encoderValues).ToArray();

            redisConnection.publisher.Publish(redisConnection.redisChannels["sim_encoder_positions"], bytes);
        }

        void Update()
        {
            if (!doneInit && controller.subscriptionDone)
                PostInit();
        }
    }
}
