// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.EventDatum.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityConsoleDiagnosticsVisualizer : MonoBehaviour, IMixedRealityConsoleDiagnosticsHandler
    {
        private int entries = 0;
        private int lastEntryIndex = 0;

        [SerializeField]
        [Tooltip("Maximum allowed entries displayed in the log console.")]
        private int maxEntries = 10;

        [SerializeField]
        [Tooltip("Text component used to render log text.")]
        private TextMeshProUGUI logText = null;

        [SerializeField]
        [Tooltip("The scroll view displaying log text.")]
        private ScrollRect logScrollView = null;

        /// <inheritdoc />
        public void OnLogReceived(ConsoleEventData eventData)
        {
            // If we reached the max entries count we'll just keep
            // the previous entry and the new one.
            string keptLog = logText.text;

            if (entries == maxEntries)
            {
                keptLog = keptLog.Substring(lastEntryIndex);
                entries = 1;
            }

            lastEntryIndex = keptLog.Length;
            var log = new StringBuilder(keptLog);
            log.AppendLine(eventData.Message);
            logText.text = log.ToString();

            // Scroll to bottom.
            logScrollView.verticalNormalizedPosition = 0f;

            entries++;
        }
    }
}