// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.EventDatum.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityFrameDiagnosticsHandler : MonoBehaviour, IMixedRealityFrameDiagnosticsHandler
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

        /// <inheritdoc />
        public void OnFrameRateChanged(FrameEventData eventData)
        {
            var framesPerSecond = eventData.FramesPerSecond;
            var stringBuilder = new StringBuilder(32);
            var millisecondStringBuilder = new StringBuilder(16);
            var displayedDecimalFormat = $"{{0:F{displayedDecimalDigits}}}";
            var milliseconds = (framesPerSecond == 0) ? 0.0f : (1.0f / framesPerSecond) * 1000.0f;

            millisecondStringBuilder.AppendFormat(displayedDecimalFormat, milliseconds.ToString(CultureInfo.InvariantCulture));

            if (eventData.IsGpuReading)
            {
                stringBuilder.AppendFormat("GPU: {0} fps ({1} ms)", framesPerSecond.ToString(), millisecondStringBuilder);
                gpuFrameRateText.text = stringBuilder.ToString();
                millisecondStringBuilder.Length = 0;
                stringBuilder.Length = 0;
            }
            else
            {
                stringBuilder.AppendFormat("CPU: {0} fps ({1} ms)", framesPerSecond.ToString(), millisecondStringBuilder);
                cpuFrameRateText.text = stringBuilder.ToString();
                stringBuilder.Length = 0;
            }
        }

        /// <inheritdoc />
        public void OnMissedFramesChanged(MissedFrameEventData eventData)
        {
            var missedFrames = eventData.MissedFrames;
            var frameIndex = missedFrames.Length - 1;

            for (int i = 0; i < missedFrameImages.Length; i++)
            {
                missedFrameImages[i].color = missedFrames[frameIndex] ? Color.red : Color.green;
                frameIndex--;
            }
        }
    }
}