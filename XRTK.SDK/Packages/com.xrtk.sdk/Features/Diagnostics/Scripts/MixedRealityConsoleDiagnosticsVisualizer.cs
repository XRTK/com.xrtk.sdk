// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
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

        /// <summary>
        /// Handler was enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            List<IMixedRealityService> services = MixedRealityToolkit.GetActiveServices<IMixedRealityDiagnosticsDataProvider>();
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] is IMixedRealityGenericDiagnosticsDataProvider<IMixedRealityConsoleDiagnosticsHandler> service)
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
                if (services[i] is IMixedRealityGenericDiagnosticsDataProvider<IMixedRealityConsoleDiagnosticsHandler> service)
                {
                    service.Unregister(this);
                }
            }
        }

        /// <inheritdoc />
        public void OnLogReceived(string message, LogType type)
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
            StringBuilder log = new StringBuilder(keptLog);
            log.AppendLine(message);
            logText.text = log.ToString();

            // Scroll to bottom.
            logScrollView.verticalNormalizedPosition = 0f;

            entries++;
        }
    }
}