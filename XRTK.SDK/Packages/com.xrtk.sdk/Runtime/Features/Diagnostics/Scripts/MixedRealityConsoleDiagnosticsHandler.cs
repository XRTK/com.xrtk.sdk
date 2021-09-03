// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using TMPro;
using UnityEngine;
using XRTK.Attributes;
using XRTK.EventDatum.DiagnosticsSystem;
using XRTK.Interfaces.DiagnosticsSystem.Handlers;

namespace XRTK.SDK.DiagnosticsSystem
{
    public class MixedRealityConsoleDiagnosticsHandler : MonoBehaviour,
        IMixedRealityConsoleDiagnosticsHandler
    {
        [SerializeField]
        private Color logEntryTextColor = Color.black;

        [SerializeField]
        private Color warningEntryTextColor = Color.yellow;

        [SerializeField]
        private Color errorEntryTextColor = Color.red;

        [SerializeField]
        [Tooltip("Maximum allowed entries displayed in the log console.")]
        private int maxEntries = 10;

        [SerializeField]
        [Prefab(typeof(TextMeshProUGUI))]
        [Tooltip("The text prefab to use for the console list entries.")]
        private GameObject logTextPrefab = null;

        [SerializeField]
        [Tooltip("The scroll view's content root transform to spawn console text entries onto.")]
        private Transform contentRoot = null;

        private MessageEntry[] textContainers;

        private class MessageEntry
        {
            public MessageEntry(MixedRealityConsoleDiagnosticsHandler handler, TextMeshProUGUI textContent)
            {
                this.handler = handler;
                this.textContent = textContent;
            }

            private readonly TextMeshProUGUI textContent;
            private readonly MixedRealityConsoleDiagnosticsHandler handler;

            public string Message
            {
                get => textContent.text;
                set
                {
                    if (textContent.text == value) { return; }

                    textContent.text = value;
                }
            }

            private LogType logType;

            public LogType LogType
            {
                get => logType;
                set
                {
                    if (logType == value) { return; }

                    switch (value)
                    {
                        case LogType.Assert:
                        case LogType.Error:
                        case LogType.Exception:
                            textContent.color = handler.errorEntryTextColor;
                            break;
                        case LogType.Warning:
                            textContent.color = handler.warningEntryTextColor;
                            break;
                        case LogType.Log:
                            textContent.color = handler.logEntryTextColor;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(value), value, null);
                    }

                    logType = value;
                }
            }
        }

        private void Awake()
        {
            textContainers = new MessageEntry[maxEntries];

            for (int i = 0; i < textContainers.Length; i++)
            {
                textContainers[i] = new MessageEntry(this, Instantiate(logTextPrefab, contentRoot).GetComponent<TextMeshProUGUI>());
            }
        }

        /// <inheritdoc />
        public void OnLogReceived(ConsoleEventData eventData)
        {
            int entryIndex;

            for (entryIndex = textContainers.Length - 1; entryIndex > 0; entryIndex--)
            {
                var entry = textContainers[entryIndex];
                var prevEntry = textContainers[entryIndex - 1];
                entry.Message = prevEntry.Message;
                entry.LogType = prevEntry.LogType;
            }

            if (entryIndex == 0)
            {
                textContainers[0].Message = eventData.Message;
                textContainers[0].LogType = eventData.LogType;
            }
        }
    }
}