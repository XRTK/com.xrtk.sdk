// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions;
using XRTK.Definitions.Controllers.Hands;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// Hand controller near interaction pointer.
    /// </summary>
    public class HandNearPointer : LinePointer
    {
        private bool handIsPinching;

        /// <inheritdoc />
        public override bool IsInteractionEnabled => base.IsInteractionEnabled && !handIsPinching;

        /// <inheritdoc />
        public override InteractionMode InteractionMode => InteractionMode.Near;

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            base.OnInputChanged(eventData);

            // This pointer type must only be used with hand controllers.
            if (!(Controller is IMixedRealityHandController handController))
            {
                Debug.LogError($"{nameof(HandNearPointer)} is only for use with {nameof(IMixedRealityHandController)} controllers!", this);
                return;
            }

            handIsPinching = handController.IsPinching;
        }
    }
}