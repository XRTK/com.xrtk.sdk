// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.EventDatum.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;
using XRTK.Services.DiagnosticsSystem;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityMemoryDiagnosticsHandler : MonoBehaviour, IMixedRealityMemoryDiagnosticsHandler
    {
        private const string usedMemoryPrefix = "Used: ";
        private const string peakMemoryPrefix = "Peak: ";
        private const string limitMemoryPrefix = "Limit: ";
        private const int maxStringLength = 32;

        private readonly char[] stringBuffer = new char[maxStringLength];

        [Range(0, 3)]
        [SerializeField]
        [Tooltip("How many decimal places to display on numeric strings.")]
        private int displayedDecimalDigits = 1;

        [SerializeField]
        [Tooltip("The text component used to display the memory usage info.")]
        private TextMeshProUGUI memoryUsedText = null;

        [SerializeField]
        [Tooltip("The text component used to display the memory peak info.")]
        private TextMeshProUGUI memoryPeakText = null;

        [SerializeField]
        [Tooltip("The text component used to display the memory limit info.")]
        private TextMeshProUGUI memoryLimitText = null;

        [SerializeField]
        [Tooltip("Slider visualizing peak memory.")]
        private Slider peakMemorySlider = null;

        [SerializeField]
        [Tooltip("Slider visualizing used memory.")]
        private Slider usedMemorySlider = null;

        private ulong lastMemoryUsage;
        private ulong lastMemoryLimit;
        private ulong lastMemoryPeak;

        private void Awake()
        {
            peakMemorySlider.minValue = 0;
            peakMemorySlider.wholeNumbers = true;
            usedMemorySlider.minValue = 0;
            usedMemorySlider.wholeNumbers = true;
        }

        #region IMixedRealityMemoryDiagnosticsHandler Implementation

        /// <inheritdoc />
        public void OnMemoryUsageChanged(MemoryEventData eventData)
        {
            ulong currentMemoryUsage = eventData.CurrentMemoryUsage;

            if (WillDisplayedMemoryDiffer(lastMemoryUsage, currentMemoryUsage, displayedDecimalDigits))
            {
                memoryUsedText.text = MemoryToString(usedMemoryPrefix, currentMemoryUsage);
                usedMemorySlider.value = DiagnosticsUtils.ConvertBytesToMegabytes(currentMemoryUsage);
                lastMemoryUsage = currentMemoryUsage;
            }
        }

        /// <inheritdoc />
        public void OnMemoryLimitChanged(MemoryEventData eventData)
        {
            ulong currentMemoryLimit = eventData.CurrentMemoryLimit;

            if (WillDisplayedMemoryDiffer(lastMemoryLimit, currentMemoryLimit, displayedDecimalDigits))
            {
                memoryLimitText.text = MemoryToString(limitMemoryPrefix, currentMemoryLimit);
                peakMemorySlider.maxValue = DiagnosticsUtils.ConvertBytesToMegabytes(currentMemoryLimit);
                usedMemorySlider.maxValue = peakMemorySlider.maxValue;
                lastMemoryLimit = currentMemoryLimit;
            }
        }

        /// <inheritdoc />
        public void OnMemoryPeakChanged(MemoryEventData eventData)
        {
            ulong currentMemoryPeak = eventData.MemoryPeak;

            if (WillDisplayedMemoryDiffer(lastMemoryPeak, currentMemoryPeak, displayedDecimalDigits))
            {
                memoryPeakText.text = MemoryToString(peakMemoryPrefix, currentMemoryPeak);
                peakMemorySlider.value = DiagnosticsUtils.ConvertBytesToMegabytes(currentMemoryPeak);
                lastMemoryPeak = currentMemoryPeak;
            }
        }

        #endregion IMixedRealityMemoryDiagnosticsHandler Implementation

        private string MemoryToString(string prefixString, ulong memory)
        {
            var bufferIndex = 0;
            // Using a custom number to string method to avoid the overhead,
            // and allocations, of built in string.Format/StringBuilder methods.
            // We can also make some assumptions since the domain of the input number (memoryUsage) is known.
            var memoryUsageMb = DiagnosticsUtils.ConvertBytesToMegabytes(memory);
            var memoryUsageIntegerDigits = (int)memoryUsageMb;
            var memoryUsageFractionalDigits = (int)((memoryUsageMb - memoryUsageIntegerDigits) * Mathf.Pow(10.0f, displayedDecimalDigits));

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