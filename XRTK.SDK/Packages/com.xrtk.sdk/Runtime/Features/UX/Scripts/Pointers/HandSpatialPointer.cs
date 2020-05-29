// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions;
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
        private IMixedRealityHandController handController;

        [SerializeField]
        private Transform pointerPoseTransform = null;

        [SerializeField]
        [Tooltip("Local offset for the pointer pose transform when not pinching.")]
        private Vector3 offsetStart = Vector3.zero;

        [SerializeField]
        [Tooltip("Local offset for the pointer pose transform when pinching.")]
        private Vector3 offsetEnd = Vector3.zero;

        /// <inheritdoc />
        public override bool IsInteractionEnabled =>
            base.IsInteractionEnabled &&
            IsNearPointerIdle &&
            HandController != null &&
            HandController.IsPointing;

        /// <summary>
        /// Gets the near pointer attached to the hand.
        /// </summary>
        private IMixedRealityPointer NearPointer => nearPointer ?? (nearPointer = InitializeNearPointerReference());

        /// <summary>
        /// Casted reference to the hand controller driving the pointer.
        /// </summary>
        private IMixedRealityHandController HandController => handController ?? (handController = InitializeHandControllerReference());

        /// <summary>
        /// Is the near pointer in an idle state where it's not
        /// interacting with anything and not targeting anything?
        /// </summary>
        private bool IsNearPointerIdle => NearPointer == null || NearPointer.Result.CurrentPointerTarget == null || !NearPointer.IsInteractionEnabled;

        private IMixedRealityHandController InitializeHandControllerReference()
        {
            // This pointer type must only be used with hand controllers.
            if (!(Controller is IMixedRealityHandController controller))
            {
                Debug.LogError($"{nameof(HandSpatialPointer)} is only for use with {nameof(IMixedRealityHandController)} controllers!", this);
                return null;
            }

            return controller;
        }

        private IMixedRealityPointer InitializeNearPointerReference()
        {
            for (int i = 0; i < Controller.InputSource.Pointers.Length; i++)
            {
                var pointer = Controller.InputSource.Pointers[i];

                if (pointer.PointerId != PointerId && pointer is HandNearPointer)
                {
                    return pointer;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override InteractionMode InteractionMode => InteractionMode.Far;

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            base.OnInputChanged(eventData);

            if (IsInteractionEnabled)
            {
                pointerPoseTransform.gameObject.SetActive(true);
                var pinchScale = Mathf.Clamp(1 - HandController.PinchStrength, .5f, 1f);
                pointerPoseTransform.localScale = new Vector3(pinchScale, pinchScale, 1f);
                pointerPoseTransform.localPosition = Vector3.Slerp(offsetStart, offsetEnd, HandController.PinchStrength);
            }
            else
            {
                pointerPoseTransform.gameObject.SetActive(false);
            }
        }
    }
}