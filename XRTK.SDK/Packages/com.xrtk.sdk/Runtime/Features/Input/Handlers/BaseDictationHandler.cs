// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Speech;
using XRTK.Services;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// Script used to start and stop recording sessions in the current dictation system.
    /// For this script to work, a dictation system like 'Windows Dictation Data Provider' must be added to the Data Providers in the Input System profile.
    /// </summary>
    public class BaseDictationHandler : BaseInputHandler, IMixedRealityDictationHandler
    {
        [SerializeField]
        [Tooltip("Time length in seconds before the dictation session ends due to lack of audio input in case there was no audio heard in the current session")]
        private float initialSilenceTimeout = 5f;

        [SerializeField]
        [Tooltip("Time length in seconds before the dictation session ends due to lack of audio input.")]
        private float autoSilenceTimeout = 20f;

        [SerializeField]
        [Tooltip("Length in seconds for the dictation service to listen")]
        private int recordingTime = 10;

        [SerializeField]
        [Tooltip("Whether recording should start automatically on start")]
        private bool startRecordingOnStart = false;

        private IMixedRealityDictationDataProvider dictationSystem;

        /// <summary>
        /// Start a recording session in the dictation system.
        /// </summary>
        public virtual void StartRecording()
        {
            dictationSystem?.StartRecording(gameObject, initialSilenceTimeout, autoSilenceTimeout, recordingTime);
        }

        /// <summary>
        /// Stop a recording session in the dictation system.
        /// </summary>
        public virtual void StopRecording()
        {
            dictationSystem?.StopRecording();
        }

        #region MonoBehaviour implementation

        protected override void Start()
        {
            base.Start();

            dictationSystem = MixedRealityToolkit.GetService<IMixedRealityDictationDataProvider>();
            Debug.Assert(dictationSystem != null, "No dictation system found. In order to use dictation, add a dictation system like 'Windows Dictation Input Provider' to the Data Providers in the Input System profile");

            if (startRecordingOnStart)
            {
                StartRecording();
            }
        }

        protected override void OnDisable()
        {
            StopRecording();

            base.OnDisable();
        }

        #endregion MonoBehaviour implementation

        #region IMixedRealityDictationHandler implementation

        /// <inheritdoc />
        public virtual void OnDictationHypothesis(DictationEventData eventData)
        {
        }

        /// <inheritdoc />
        public virtual void OnDictationResult(DictationEventData eventData)
        {
        }

        /// <inheritdoc />
        public virtual void OnDictationComplete(DictationEventData eventData)
        {
        }

        /// <inheritdoc />
        public virtual void OnDictationError(DictationEventData eventData)
        {
        }

        #endregion IMixedRealityDictationHandler implementation
    }
}