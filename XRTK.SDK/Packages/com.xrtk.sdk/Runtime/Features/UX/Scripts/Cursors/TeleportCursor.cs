// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.Interfaces.CameraSystem;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.LocomotionSystem;
using XRTK.SDK.UX.Pointers;
using XRTK.Services;
using XRTK.Services.LocomotionSystem;
using XRTK.Utilities;

namespace XRTK.SDK.UX.Cursors
{
    public class TeleportCursor : AnimatedCursor, IMixedRealityLocomotionSystemHandler
    {
        [SerializeField]
        [Tooltip("Arrow Transform to point in the Teleporting direction.")]
        private Transform arrowTransform = null;

        private Vector3 cursorOrientation = Vector3.zero;

        #region IMixedRealityCursor Implementation

        /// <inheritdoc />
        public override IMixedRealityPointer Pointer
        {
            get => pointer;
            set
            {
                Debug.Assert(value.GetType() == typeof(TeleportPointer) ||
                             value.GetType() == typeof(ParabolicTeleportPointer),
                    "Teleport Cursor's Pointer must derive from a TeleportPointer type.");

                pointer = (TeleportPointer)value;
                pointer.BaseCursor = this;
                RegisterManagers();
            }
        }

        private TeleportPointer pointer;

        /// <inheritdoc />
        public override Vector3 Position => PrimaryCursorVisual.position;

        /// <inheritdoc />
        public override Quaternion Rotation => arrowTransform.rotation;

        /// <inheritdoc />
        public override Vector3 LocalScale => PrimaryCursorVisual.localScale;

        /// <inheritdoc />
        public override CursorStateEnum CheckCursorState()
        {
            if (CursorState != CursorStateEnum.Contextual)
            {
                if (pointer.IsInteractionEnabled)
                {
                    switch (pointer.ValidationResult)
                    {
                        case TeleportValidationResult.None:
                            return CursorStateEnum.Release;
                        case TeleportValidationResult.Invalid:
                            return CursorStateEnum.ObserveHover;
                        case TeleportValidationResult.Valid:
                            return CursorStateEnum.ObserveHover;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return CursorStateEnum.Release;
            }

            return CursorStateEnum.Contextual;
        }

        /// <inheritdoc />
        protected override void UpdateCursorTransform()
        {
            if (Pointer == null)
            {
                Debug.LogError($"[TeleportCursor.{name}] No Pointer has been assigned!");
                Destroy(gameObject);
                return;
            }

            if (!InputSystem.FocusProvider.TryGetFocusDetails(Pointer, out var focusDetails))
            {
                if (InputSystem.FocusProvider.IsPointerRegistered(Pointer))
                {
                    Debug.LogError($"{gameObject.name}: Unable to get focus details for {pointer.GetType().Name}!");
                }
                else
                {
                    Debug.LogError($"{pointer.GetType().Name} has not been registered!");
                    Destroy(gameObject);
                }

                return;
            }

            transform.position = focusDetails.EndPoint;

            var cameraTransform = MixedRealityToolkit.TryGetSystem<IMixedRealityCameraSystem>(out var cameraSystem)
                ? cameraSystem.MainCameraRig.CameraTransform
                : CameraCache.Main.transform;
            var forward = cameraTransform.forward;
            forward.y = 0f;

            // Smooth out rotation just a tad to prevent jarring transitions
            PrimaryCursorVisual.rotation = Quaternion.Lerp(PrimaryCursorVisual.rotation, Quaternion.LookRotation(forward.normalized, Vector3.up), 0.5f);

            // Point the arrow towards the target orientation
            cursorOrientation.y = pointer.PointerOrientation;
            arrowTransform.eulerAngles = cursorOrientation;
        }

        #endregion IMixedRealityCursor Implementation

        #region IMixedRealityLocomotionSystemHandler Implementation

        /// <inheritdoc />
        public void OnTeleportTargetRequested(LocomotionEventData eventData)
        {
            OnCursorStateChange(CursorStateEnum.Observe);
        }

        /// <inheritdoc />
        public void OnTeleportStarted(LocomotionEventData eventData)
        {
            OnCursorStateChange(CursorStateEnum.Release);
        }

        /// <inheritdoc />
        public void OnTeleportCompleted(LocomotionEventData eventData) { }

        /// <inheritdoc />
        public void OnTeleportCanceled(LocomotionEventData eventData)
        {
            OnCursorStateChange(CursorStateEnum.Release);
        }

        #endregion IMixedRealityLocomotionSystemHandler Implementation
    }
}
