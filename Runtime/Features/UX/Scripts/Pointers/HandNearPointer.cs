// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions;
using XRTK.Definitions.Physics;
using XRTK.Extensions;
using XRTK.Interfaces.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// Hand controller near interaction pointer.
    /// </summary>
    public class HandNearPointer : BaseControllerPointer
    {
        private IMixedRealityHandController handController;

        /// <inheritdoc />
        public override bool IsInteractionEnabled => base.IsInteractionEnabled && !HandController.IsPinching;

        /// <inheritdoc />
        public override InteractionMode InteractionMode => InteractionMode.Near;

        /// <summary>
        /// Casted reference to the hand controller driving the pointer.
        /// </summary>
        private IMixedRealityHandController HandController => handController ?? (handController = InitializeHandControllerReference());

        private IMixedRealityHandController InitializeHandControllerReference()
        {
            // This pointer type must only be used with hand controllers.
            if (!(Controller is IMixedRealityHandController controller))
            {
                Debug.LogError($"{nameof(HandNearPointer)} is only for use with {nameof(IMixedRealityHandController)} controllers!", this);
                return null;
            }

            return controller;
        }

        /// <inheritdoc />
        public override void OnPreRaycast()
        {
            if (Rays == null || Rays.Length > 1)
            {
                Rays = new RayStep[1];
            }

            var origin = TryGetPointerPosition(out var pointerPosition) ? pointerPosition : Vector3.zero;
            var terminus = CapturedNearInteractionObject.IsNull() ? origin + Vector3.forward : origin + CapturedNearInteractionObject.transform.forward;

            Rays[0] = new RayStep();
            Rays[0].UpdateRayStep(ref origin, ref terminus);
        }
    }
}