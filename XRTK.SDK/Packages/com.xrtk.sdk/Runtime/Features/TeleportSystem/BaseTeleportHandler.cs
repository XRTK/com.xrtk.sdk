// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Teleport;
using XRTK.Interfaces.TeleportSystem;
using XRTK.Interfaces.TeleportSystem.Handlers;
using XRTK.Services;

namespace XRTK.SDK.TeleportSystem
{
    /// <summary>
    /// Base implementation for handling <see cref="IMixedRealityTeleportSystem"/> events in a
    /// <see cref="MonoBehaviour"/> component.
    /// </summary>
    public abstract class BaseTeleportHandler : MonoBehaviour, IMixedRealityTeleportComponentHandler
    {
        private IMixedRealityTeleportSystem teleportSystem;

        /// <summary>
        /// Gets the active <see cref="IMixedRealityTeleportSystem"/> implementation instance.
        /// </summary>
        protected IMixedRealityTeleportSystem TeleportSystem => teleportSystem ?? (teleportSystem = MixedRealityToolkit.GetService<IMixedRealityTeleportSystem>());

        /// <summary>
        /// This method is called when the behaviour becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (MixedRealityToolkit.Instance.ActiveProfile.IsTeleportSystemEnabled)
            {
                MixedRealityToolkit.TeleportSystem.Register(gameObject);
            }
        }

        /// <summary>
        /// This method is called when the behaviour becomes disabled and inactive.
        /// </summary>
        protected virtual void OnDisable()
        {
            MixedRealityToolkit.TeleportSystem?.Unregister(gameObject);
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
