// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.InputSystem.Controllers.Hands;
using XRTK.Services;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// Pointer class for hand controller pointers.
    /// </summary>
    public class HandPointer : BaseControllerPointer
    {
        private IMixedRealityHandRay handRay;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            if (handRay == null)
            {
                InitializeHandRay();
            }
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            if (handRay == null)
            {
                InitializeHandRay();
            }
        }

        /// <inheritdoc />
        public override bool TryGetPointerPosition(out Vector3 position)
        {
            return base.TryGetPointerPosition(out position);
        }

        /// <inheritdoc />
        public override bool TryGetPointerRotation(out Quaternion rotation)
        {
            return base.TryGetPointerRotation(out rotation);
        }

        /// <inheritdoc />
        public override bool TryGetPointingRay(out Ray pointingRay)
        {
            return base.TryGetPointingRay(out pointingRay);
        }

        private void InitializeHandRay()
        {
            SystemType handRayType = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.HandTrackingProfile.HandRayType;
            handRay = (IMixedRealityHandRay)Activator.CreateInstance(handRayType.Type);
        }
    }
}