// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.EventDatum.Teleport;
using XRTK.Interfaces.TeleportSystem;

namespace XRTK.SDK.TeleportSystem
{
    /// <summary>
    /// This <see cref="IMixedRealityTeleportSystem"/> handler implementation will
    /// fade out the camera when teleporting and fade it back in when done.
    /// </summary>
    [System.Runtime.InteropServices.Guid("0db5b0fd-9ac3-487a-abfd-754963f4e2a3")]
    public class FadingTeleportHandler : BaseTeleportHandler
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
