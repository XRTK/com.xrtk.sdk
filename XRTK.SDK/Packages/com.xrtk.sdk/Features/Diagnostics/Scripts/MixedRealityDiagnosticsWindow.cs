// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using TMPro;
using UnityEngine;
using XRTK.SDK.Utilities.Solvers;
using XRTK.Services;
using XRTK.Utilities.Async;

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

        private void OnValidate()
        {
            if (solverHandler == null)
            {
                solverHandler = GetComponent<SolverHandler>();
            }
        }

        /// <summary>
        /// The diagnostics window was enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (solverHandler == null)
            {
                solverHandler = GetComponent<SolverHandler>();
            }

            applicationSignatureText.text = MixedRealityToolkit.DiagnosticsSystem.ApplicationSignature;
        }

        /// <summary>
        /// Pins or unpins the diagnostics window at its current location.
        /// </summary>
        public void Toggle_PinWindow()
        {
            solverHandler.enabled = !solverHandler.enabled;
        }
    }
}