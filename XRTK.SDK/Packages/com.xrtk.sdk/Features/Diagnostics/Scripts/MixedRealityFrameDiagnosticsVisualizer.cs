// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.Interfaces;
using XRTK.Interfaces.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;
using XRTK.Services;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityFrameDiagnosticsVisualizer : MonoBehaviour, IMixedRealityFrameDiagnosticsHandler
    {
        [Range(0, 3)]
        [SerializeField]
        [Tooltip("How many decimal places to display on numeric strings.")]
        private int displayedDecimalDigits = 1;

        [SerializeField]
        [Tooltip("The text component used to display the CPU FPS information.")]
        private TextMeshProUGUI cpuFrameRateText = null;

        [SerializeField]
        [Tooltip("The text component used to display the GPU FPS information.")]
        private TextMeshProUGUI gpuFrameRateText = null;

        [SerializeField]
        [Tooltip("Image components used to visualize missed frames.")]
        private Image[] missedFrameImages = null;

        /// <summary>
        /// Handler was enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            List<IMixedRealityService> services = MixedRealityToolkit.GetActiveServices<IMixedRealityDiagnosticsDataProvider>();
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] is IMixedRealityGenericDiagnosticsDataProvider<IMixedRealityFrameDiagnosticsHandler> service)
                {
                    service.Register(this);
                }
            }
        }

        /// <summary>
        /// Handler was disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            List<IMixedRealityService> services = MixedRealityToolkit.GetActiveServices<IMixedRealityDiagnosticsDataProvider>();
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] is IMixedRealityGenericDiagnosticsDataProvider<IMixedRealityFrameDiagnosticsHandler> service)
                {
                    service.Unregister(this);
                }
            }
        }

        /// <inheritdoc />
        public void OnMissedFramesChanged(bool[] missedFrames)
        {
            int frameIndex = missedFrames.Length - 1;
            for (int i = 0; i < missedFrameImages.Length; i++)
            {
                missedFrameImages[i].color = missedFrames[frameIndex] ? Color.red : Color.green;
                frameIndex--;
            }
        }

        /// <inheritdoc />
        public void OnFrameRateChanged(int oldFPS, int newFPS, bool isGPU)
        {
            string displayedDecimalFormat = $"{{0:F{displayedDecimalDigits}}}";

            StringBuilder stringBuilder = new StringBuilder(32);
            StringBuilder millisecondStringBuilder = new StringBuilder(16);

            float milliseconds = (newFPS == 0) ? 0.0f : (1.0f / newFPS) * 1000.0f;
            millisecondStringBuilder.AppendFormat(displayedDecimalFormat, milliseconds.ToString(CultureInfo.InvariantCulture));

            // GPU
            if (isGPU)
            {
                stringBuilder.AppendFormat("GPU: {0} fps ({1} ms)", newFPS.ToString(), millisecondStringBuilder);
                gpuFrameRateText.text = stringBuilder.ToString();
                millisecondStringBuilder.Length = 0;
                stringBuilder.Length = 0;
            }
            else
            {
                // CPU
                stringBuilder.AppendFormat("CPU: {0} fps ({1} ms)", newFPS.ToString(), millisecondStringBuilder);
                cpuFrameRateText.text = stringBuilder.ToString();
                stringBuilder.Length = 0;
            }
        }
    }
}