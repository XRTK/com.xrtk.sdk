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
    public class MixedRealityFrameDiagnosticsHandler : MonoBehaviour,
        IMixedRealityFrameDiagnosticsHandler
    {
        private const string CPU = "CPU: {0} fps ({1} ms)";
        private const string GPU = "GPU: {0} fps ({1} ms)";

        private readonly StringBuilder stringBuilder = new StringBuilder(32);
        private readonly StringBuilder millisecondStringBuilder = new StringBuilder(16);

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

        private string displayDecimalFormat = null;

        private string DisplayedDecimalFormat => displayDecimalFormat ?? (displayDecimalFormat = $"{{0:F{displayedDecimalDigits}}}");

        /// <inheritdoc />
        public void OnFrameRateChanged(FrameEventData eventData)
        {
            var framesPerSecond = eventData.FramesPerSecond;
            var milliseconds = framesPerSecond == 0 ? 0.0f : (1.0f / framesPerSecond) * 1000.0f;

            millisecondStringBuilder.AppendFormat(DisplayedDecimalFormat, milliseconds.ToString(CultureInfo.InvariantCulture));

            if (eventData.IsGpuReading)
            {
                stringBuilder.AppendFormat(GPU, framesPerSecond.ToString(), millisecondStringBuilder);
                gpuFrameRateText.text = stringBuilder.ToString();
            }
            else
            {
                stringBuilder.AppendFormat(CPU, framesPerSecond.ToString(), millisecondStringBuilder);
                cpuFrameRateText.text = stringBuilder.ToString();
            }

            millisecondStringBuilder.Clear();
            stringBuilder.Clear();
        }

        /// <inheritdoc />
        public void OnFrameMissed(FrameEventData eventData)
        {
            var missedFrames = eventData.MissedFrames;
            var frameIndex = missedFrames.Length - 1;

            foreach (var frame in missedFrameImages)
            {
                frame.color = missedFrames[frameIndex] ? Color.red : Color.green;
                frameIndex--;
            }
        }
    }
}