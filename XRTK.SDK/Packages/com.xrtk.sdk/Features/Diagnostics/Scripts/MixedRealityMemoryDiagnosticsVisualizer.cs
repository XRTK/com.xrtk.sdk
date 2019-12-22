// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.Interfaces;
using XRTK.Interfaces.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;
using XRTK.Services;
using XRTK.Services.DiagnosticsSystem;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityMemoryDiagnosticsVisualizer : MonoBehaviour, IMixedRealityMemoryDiagnosticsHandler
    {
        private static readonly string usedMemoryPrefix = "Used: ";
        private static readonly string peakMemoryPrefix = "Peak: ";
        private static readonly string limitMemoryPrefix = "Limit: ";
        private static readonly int maxStringLength = 32;
        private readonly char[] stringBuffer = new char[maxStringLength];

        [Range(0, 3)]
        [SerializeField]
        [Tooltip("How many decimal places to display on numeric strings.")]
        private int displayedDecimalDigits = 1;

        [SerializeField]
        [Tooltip("The text component used to display the memory usage info.")]
        private TextMeshProUGUI memoryUsedText;

        [SerializeField]
        [Tooltip("The text component used to display the memory peak info.")]
        private TextMeshProUGUI memoryPeakText;

        [SerializeField]
        [Tooltip("The text component used to display the memory limit info.")]
        private TextMeshProUGUI memoryLimitText;

        [SerializeField]
        [Tooltip("Slider visuailzing peak memory.")]
        private Slider peakMemorySlider;

        [SerializeField]
        [Tooltip("Slider visuailzing used memory.")]
        private Slider usedMemorySlider;

        private void Awake()
        {
            peakMemorySlider.minValue = 0;
            peakMemorySlider.wholeNumbers = true;
            usedMemorySlider.minValue = 0;
            usedMemorySlider.wholeNumbers = true;
        }

        /// <summary>
        /// Handler was enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            List<IMixedRealityService> services = MixedRealityToolkit.GetActiveServices<IMixedRealityDiagnosticsDataProvider>();
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] is IMixedRealityGenericDiagnosticsDataProvider<IMixedRealityMemoryDiagnosticsHandler> service)
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
                if (services[i] is IMixedRealityGenericDiagnosticsDataProvider<IMixedRealityMemoryDiagnosticsHandler> service)
                {
                    service.Unregister(this);
                }
            }
        }

        /// <inheritdoc />
        public void OnMemoryUsageChanged(ulong oldMemoryUsage, ulong newMemoryUsage)
        {
            if (WillDisplayedMemoryDiffer(oldMemoryUsage, newMemoryUsage, displayedDecimalDigits))
            {
                memoryUsedText.text = MemoryToString(stringBuffer, displayedDecimalDigits, usedMemoryPrefix, newMemoryUsage);
                usedMemorySlider.value = DiagnosticsUtils.ConvertBytesToMegabytes(newMemoryUsage);
            }
        }

        /// <inheritdoc />
        public void OnMemoryLimitChanged(ulong oldMemoryLimit, ulong newMemoryLimit)
        {
            if (WillDisplayedMemoryDiffer(oldMemoryLimit, newMemoryLimit, displayedDecimalDigits))
            {
                memoryLimitText.text = MemoryToString(stringBuffer, displayedDecimalDigits, limitMemoryPrefix, newMemoryLimit);
                peakMemorySlider.maxValue = DiagnosticsUtils.ConvertBytesToMegabytes(newMemoryLimit);
                usedMemorySlider.maxValue = peakMemorySlider.maxValue;
            }
        }

        /// <inheritdoc />
        public void OnMemoryPeakChanged(ulong oldMemoryPeak, ulong newMemoryPeak)
        {
            if (WillDisplayedMemoryDiffer(oldMemoryPeak, newMemoryPeak, displayedDecimalDigits))
            {
                memoryPeakText.text = MemoryToString(stringBuffer, displayedDecimalDigits, peakMemoryPrefix, newMemoryPeak);
                peakMemorySlider.value = DiagnosticsUtils.ConvertBytesToMegabytes(newMemoryPeak);
            }
        }

        private string MemoryToString(char[] stringBuffer, int displayedDecimalDigits, string prefixString, ulong memory)
        {
            // Using a custom number to string method to avoid the overhead, and allocations, of built in string.Format/StringBuilder methods.
            // We can also make some assumptions since the domain of the input number (memoryUsage) is known.
            var memoryUsageMb = DiagnosticsUtils.ConvertBytesToMegabytes(memory);
            int memoryUsageIntegerDigits = (int)memoryUsageMb;
            int memoryUsageFractionalDigits = (int)((memoryUsageMb - memoryUsageIntegerDigits) * Mathf.Pow(10.0f, displayedDecimalDigits));
            int bufferIndex = 0;

            for (int i = 0; i < prefixString.Length; ++i)
            {
                stringBuffer[bufferIndex++] = prefixString[i];
            }

            bufferIndex = MemoryItoA(memoryUsageIntegerDigits, stringBuffer, bufferIndex);
            stringBuffer[bufferIndex++] = '.';

            if (memoryUsageFractionalDigits != 0)
            {
                bufferIndex = MemoryItoA(memoryUsageFractionalDigits, stringBuffer, bufferIndex);
            }
            else
            {
                for (int i = 0; i < displayedDecimalDigits; ++i)
                {
                    stringBuffer[bufferIndex++] = '0';
                }
            }

            stringBuffer[bufferIndex++] = 'M';
            stringBuffer[bufferIndex++] = 'B';

            return new string(stringBuffer, 0, bufferIndex);
        }

        private static int MemoryItoA(int value, char[] stringBuffer, int bufferIndex)
        {
            int startIndex = bufferIndex;

            for (; value != 0; value /= 10)
            {
                stringBuffer[bufferIndex++] = (char)((char)(value % 10) + '0');
            }

            for (int endIndex = bufferIndex - 1; startIndex < endIndex; ++startIndex, --endIndex)
            {
                var temp = stringBuffer[startIndex];
                stringBuffer[startIndex] = stringBuffer[endIndex];
                stringBuffer[endIndex] = temp;
            }

            return bufferIndex;
        }

        private static bool WillDisplayedMemoryDiffer(ulong oldUsage, ulong newUsage, int displayedDecimalDigits)
        {
            var oldUsageMBs = DiagnosticsUtils.ConvertBytesToMegabytes(oldUsage);
            var newUsageMBs = DiagnosticsUtils.ConvertBytesToMegabytes(newUsage);
            var decimalPower = Mathf.Pow(10.0f, displayedDecimalDigits);

            return (int)(oldUsageMBs * decimalPower) != (int)(newUsageMBs * decimalPower);
        }
    }
}