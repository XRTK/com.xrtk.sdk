// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.EventDatum.Teleport;
using XRTK.Interfaces.TeleportSystem;

namespace XRTK.SDK.TeleportSystem
{
    /// <summary>
    /// This <see cref="IMixedRealityTeleportSystem"/> handler implementation will
    /// perform an instant teleport to the target location.
    /// </summary>
    [System.Runtime.InteropServices.Guid("cb3ee7a8-114e-44d6-9edc-cd55049fefb6")]
    public class InstantTeleportHandler : BaseTeleportHandler
    {
        /// <inheritdoc />
        public override void OnTeleportCanceled(TeleportEventData eventData) { }

        /// <inheritdoc />
        public override void OnTeleportCompleted(TeleportEventData eventData) { }

        /// <inheritdoc />
        public override void OnTeleportRequest(TeleportEventData eventData) { }

        /// <inheritdoc />
        public override void OnTeleportStarted(TeleportEventData eventData) { }
    }
}
