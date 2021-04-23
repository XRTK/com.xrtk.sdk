// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRTK.Interfaces.DiagnosticsSystem;
using XRTK.SDK.Utilities.Solvers;
using XRTK.Services;

namespace XRTK.SDK.DiagnosticsSystem
{
    [RequireComponent(typeof(SolverHandler))]
    public class MixedRealityDiagnosticsWindow : MonoBehaviour
    {
        [SerializeField]
        private SolverHandler solverHandler = null;

        [SerializeField]
        [Tooltip("The text component used to display the application build version and identifier.")]
        private TextMeshProUGUI applicationSignatureText = null;

        [SerializeField]
        [Tooltip("The icon reference to swap out the pin button sprite.")]
        private Image icon = null;

        [SerializeField]
        [Tooltip("The pin sprite to use when the window is unpinned.")]
        private Sprite pinGraphic = null;

        [SerializeField]
        [Tooltip("The unpin graphic to use when the window is pinned in place.")]
        private Sprite unPinGraphic = null;

        /// <summary>
        /// Is the diagnostics windows pinned in space?
        /// </summary>
        public bool IsPinned => solverHandler.enabled;

        private IMixedRealityDiagnosticsSystem diagnosticsSystem;

        private IMixedRealityDiagnosticsSystem DiagnosticsSystem
            => diagnosticsSystem ?? (diagnosticsSystem = MixedRealityToolkit.GetSystem<IMixedRealityDiagnosticsSystem>());

        protected virtual void OnValidate()
        {
            if (solverHandler == null)
            {
                solverHandler = GetComponent<SolverHandler>();
            }
        }

        protected virtual void OnEnable()
        {
            if (solverHandler == null)
            {
                solverHandler = GetComponent<SolverHandler>();
            }

            applicationSignatureText.text = DiagnosticsSystem.ApplicationSignature;
        }

        /// <summary>
        /// Pins or unpins the diagnostics window at its current location.
        /// </summary>
        public void Toggle_PinWindow()
        {
            var newState = !solverHandler.enabled;
            icon.sprite = newState ? pinGraphic : unPinGraphic;
            solverHandler.enabled = newState;
        }
    }
}