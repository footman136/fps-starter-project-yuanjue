﻿using UnityEngine;

namespace Fps
{
    [RequireComponent(typeof(Camera))]
    public class CameraClipQuality : MonoBehaviour
    {
        [SerializeField] private float checkoutBias = 10.0f;

        private void Awake()
        {
            var camera = GetComponent<Camera>();
            camera.farClipPlane = MapQualitySettings.CheckoutDistance + checkoutBias;
        }
    }
}
