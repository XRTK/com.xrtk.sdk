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

        [SerializeField]
        [Tooltip("The scroll view's content root transform to spawn console text entries onto.")]
        private Transform contentRoot = null;

        private TextMeshProUGUI[] textContainers;

        private void Awake()
        {
            textContainers = new TextMeshProUGUI[maxEntries];

            for (int i = 0; i < textContainers.Length; i++)
            {
                textContainers[i] = Instantiate(logTextPrefab, contentRoot).GetComponent<TextMeshProUGUI>();
            }
        }

        /// <inheritdoc />
        public void OnLogReceived(ConsoleEventData eventData)
        {
            int entryIndex;

            for (entryIndex = textContainers.Length - 1; entryIndex > 0; entryIndex--)
            {
                textContainers[entryIndex].text = textContainers[entryIndex - 1].text;
            }

            if (entryIndex == 0)
            {
                textContainers[entryIndex].text = eventData.Message;
            }

            logScrollView.verticalNormalizedPosition = 0f;
        }
    }
}