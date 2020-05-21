// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers.Hands;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// Extends the simple line pointer for drawing lines from the input source origin to the current pointer position,
    /// by adding visual state
    /// </summary>
    public class HandSpatialPointer : LinePointer
    {
        private IMixedRealityPointer nearPointer;
        private bool handIsPointing;

        /// <inheritdoc />
        public override bool IsInteractionEnabled => base.IsInteractionEnabled && IsNearPointerIdle && handIsPointing;

        /// <summary>
        /// Gets the near pointer attached to the hand.
        /// </summary>
        private IMixedRealityPointer NearPointer => nearPointer ?? (nearPointer = InitializeNearPointerReference());

        /// <summary>
        /// Is the near pointer in an idle state where it's not
        /// interacting with anything and not targeting anything?
        /// </summary>
        private bool IsNearPointerIdle => NearPointer != null && NearPointer.Result.CurrentPointerTarget == null;

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            base.OnInputChanged(eventData);

            // This pointer type must only be used with hand controllers.
            if (!(Controller is IMixedRealityHandController handController))
            {
                Debug.LogError($"{typeof(HandSpatialPointer).Name} is only for use with {typeof(IMixedRealityHandController).Name} controllers!", this);
                return;
            }

            handIsPointing = handController.IsPointing;
        }

        private IMixedRealityPointer InitializeNearPointerReference()
        {
            for (int i = 0; i < Controller.InputSource.Pointers.Length; i++)
            {
                var pointer = Controller.InputSource.Pointers[i];
                if (!pointer.PointerId.Equals(PointerId) && pointer is HandNearPointer)
                {
                    return pointer;
                }
            }

            return null;
        }
    }
}