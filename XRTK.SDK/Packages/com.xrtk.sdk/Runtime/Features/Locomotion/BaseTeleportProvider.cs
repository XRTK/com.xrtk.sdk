// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Teleport;
using XRTK.Interfaces.LocomotionSystem;
using XRTK.Services;

namespace XRTK.SDK.Locomotion
{
    /// <summary>
    /// Base implementation for handling <see cref="IMixedRealityLocomotionSystem"/> teleport events in a
    /// <see cref="MonoBehaviour"/> component.
    /// </summary>
    public abstract class BaseTeleportProvider : MonoBehaviour, IMixedRealityTeleportProvider
    {
        private IMixedRealityLocomotionSystem locomotionSystem;

        /// <summary>
        /// Gets the active <see cref="IMixedRealityLocomotionSystem"/> implementation instance.
        /// </summary>
        protected IMixedRealityLocomotionSystem LocomotionSystem => locomotionSystem ?? (locomotionSystem = MixedRealityToolkit.GetService<IMixedRealityLocomotionSystem>());

        /// <summary>
        /// This method is called when the behaviour becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (MixedRealityToolkit.Instance.ActiveProfile.IsLocomotionSystemEnabled)
            {
                MixedRealityToolkit.LocomotionSystem.Register(gameObject);
            }
        }

        /// <summary>
        /// This method is called when the behaviour becomes disabled and inactive.
        /// </summary>
        protected virtual void OnDisable()
        {
            MixedRealityToolkit.LocomotionSystem?.Unregister(gameObject);
        }

        /// <inheritdoc />
        public virtual void OnTeleportCanceled(TeleportEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnTeleportCompleted(TeleportEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnTeleportRequest(TeleportEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnTeleportStarted(TeleportEventData eventData) { }
    }
}
