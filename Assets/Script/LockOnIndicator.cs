using System.Collections;
using UnityEngine;


    public class LockOnIndicator : MonoBehaviour
    {
        public bool rotateIndicator = true;
        public float rotationSpeed = 100f;

        public bool pulseIndicator = true;
        public float pulseSpeed = 2f;
        public float pulseMin = 0.8f;
        public float pulseMax = 1.2f;

        private Vector3 initialScale;

        void Start()
        {
            initialScale = transform.localScale;
        }

        void Update()
        {
            if (rotateIndicator)
            {
                transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }

            if (pulseIndicator)
            {
                float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
                transform.localScale = initialScale * scale;
            }
        }
    }
