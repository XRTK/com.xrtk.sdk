// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers.Hands;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// Extends the simple line pointer for drawing lines from the input source origin to the current pointer position,
    /// by adding visual state
    /// </summary>
    public class HandRayPointer : LinePointer
    {
        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            base.OnInputChanged(eventData);

            // We listen for hand data changes here, so we can adjust the pointer's
            // state if needed.
            if (Controller is IMixedRealityHandController handController)
            {
                enabled = handController.IsPointing;
            }
            else
            {
                Debug.LogError($"{typeof(HandRayPointer).Name} is only for use with {typeof(IMixedRealityHandController).Name} controllers!", this);
            }
        }
    }
}