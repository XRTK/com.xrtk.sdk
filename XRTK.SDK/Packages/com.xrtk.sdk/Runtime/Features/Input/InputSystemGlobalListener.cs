﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Services;

namespace XRTK.SDK.Input
{
    /// <summary>
    /// This component ensures that all input events are forwarded to this <see cref="GameObject"/> when focus or gaze is not required.
    /// </summary>
    public class InputSystemGlobalListener : MonoBehaviour
    {
        private bool lateInitialize = true;

        protected virtual void OnEnable()
        {
            if (!lateInitialize &&
                MixedRealityToolkit.IsInitialized &&
                MixedRealityToolkit.InputSystem != null)
            {
                MixedRealityToolkit.InputSystem.Register(gameObject);
            }
        }

        protected virtual async void Start()
        {
            if (lateInitialize &&
                await MixedRealityToolkit.ValidateInputSystemAsync())
            {
                // We've been destroyed during the await.
                if (this == null) { return; }

                lateInitialize = false;
                MixedRealityToolkit.InputSystem.Register(gameObject);
            }
        }

        protected virtual void OnDisable()
        {
            MixedRealityToolkit.InputSystem?.Unregister(gameObject);
        }

        protected virtual void OnDestroy()
        {
            MixedRealityToolkit.InputSystem?.Unregister(gameObject);
        }
    }
}
