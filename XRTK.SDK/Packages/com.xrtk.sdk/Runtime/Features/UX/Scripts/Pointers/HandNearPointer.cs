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
    /// Hand controller near interaction pointer.
    /// </summary>
    public class HandNearPointer : LinePointer
    {
        private IMixedRealityPointer spatialPointer;
        private bool handIsPinching;

        /// <inheritdoc />
        public override bool IsInteractionEnabled => base.IsInteractionEnabled && !handIsPinching && false;

        ///// <summary>
        ///// Gets the near pointer attached to the hand.
        ///// </summary>
        private IMixedRealityPointer SpatialPointer => spatialPointer ?? (spatialPointer = InitializeSpatialPointerReference());

        ///// <summary>
        ///// Is the near pointer in an idle state where it's not
        ///// interacting with anything and not targeting anything?
        ///// </summary>
        //private bool IsNearPointerIdle => NearPointer != null && NearPointer.Result.CurrentPointerTarget == null;

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            base.OnInputChanged(eventData);

            // This pointer type must only be used with hand controllers.
            if (!(Controller is IMixedRealityHandController handController))
            {
                Debug.LogError($"{typeof(HandNearPointer).Name} is only for use with {typeof(IMixedRealityHandController).Name} controllers!", this);
                return;
            }

            handIsPinching = handController.IsPinching;
        }

        private IMixedRealityPointer InitializeSpatialPointerReference()
        {
            for (int i = 0; i < Controller.InputSource.Pointers.Length; i++)
            {
                var pointer = Controller.InputSource.Pointers[i];
                if (!pointer.PointerId.Equals(PointerId) && pointer is HandSpatialPointer)
                {
                    return pointer;
                }
            }

            return null;
        }
    }
}