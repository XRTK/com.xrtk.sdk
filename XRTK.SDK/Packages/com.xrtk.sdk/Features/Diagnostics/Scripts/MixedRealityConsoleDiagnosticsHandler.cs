// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.Attributes;
using XRTK.EventDatum.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityConsoleDiagnosticsHandler : MonoBehaviour,
        IMixedRealityConsoleDiagnosticsHandler
    {
        private int lastEntryIndex = 0;

        [SerializeField]
        [Tooltip("Maximum allowed entries displayed in the log console.")]
        private int maxEntries = 10;

        [SerializeField]
        [Prefab(typeof(TextMeshProUGUI))]
        [Tooltip("The text prefab to use for the console list entries.")]
        private GameObject logTextPrefab = null;

        [SerializeField]
        [Tooltip("The scroll view displaying log text.")]
        private ScrollRect logScrollView = null;

        private TextMeshProUGUI[] textContainers;

        private void Awake()
        {
            textContainers = new TextMeshProUGUI[maxEntries];

            for (int i = 0; i < textContainers.Length; i++)
            {
                textContainers[i] = Instantiate(logTextPrefab, transform).GetComponent<TextMeshProUGUI>();
            }
        }

        /// <inheritdoc />
        public void OnLogReceived(ConsoleEventData eventData)
        {
            // TODO fancy modulo to assign the next text prefab and scroll to it's position.

            // Scroll to bottom.
            logScrollView.verticalNormalizedPosition = 0f;

            lastEntryIndex++;
        }
    }
}