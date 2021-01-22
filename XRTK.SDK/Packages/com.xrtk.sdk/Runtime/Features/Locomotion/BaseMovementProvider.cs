// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Services;
using XRTK.SDK.Input;
using XRTK.Interfaces.LocomotionSystem;

namespace XRTK.SDK.Locomotion
{
    /// <summary>
    /// Base implementation for handling <see cref="IMixedRealityLocomotionSystem"/> movement events in a
    /// <see cref="MonoBehaviour"/> component.
    /// </summary>
    public abstract class BaseMovementProvider : InputSystemGlobalListener, IMixedRealityMovementProvider
    {
        private IMixedRealityLocomotionSystem locomotionSystem;

        /// <summary>
        /// Gets the active <see cref="IMixedRealityLocomotionSystem"/> implementation instance.
        /// </summary>
        protected IMixedRealityLocomotionSystem LocomotionSystem => locomotionSystem ?? (locomotionSystem = MixedRealityToolkit.GetService<IMixedRealityLocomotionSystem>());
    }
}