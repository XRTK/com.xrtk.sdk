// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.Serialization;
using XRTK.Definitions.Physics;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.LocomotionSystem;
using XRTK.Services;
using XRTK.Services.LocomotionSystem;
using XRTK.Utilities.Physics;

namespace XRTK.SDK.UX.Pointers
{
    public class TeleportPointer : LinePointer, ITeleportTargetProvider
    {
        [SerializeField]
        [FormerlySerializedAs("LineColorHotSpot")]
        private Gradient lineColorHotSpot = new Gradient();

        protected Gradient LineColorHotSpot
        {
            get => lineColorHotSpot;
            set => lineColorHotSpot = value;
        }

        //private Vector2 currentDualAxisInputPosition = Vector2.zero;
        private bool teleportEnabled = false;
        //private bool canTeleport = false;
        //private bool canMove = false;

        private ITeleportValidationProvider validationDataProvider;
        private ITeleportValidationProvider ValidationDataProvider => validationDataProvider ?? (validationDataProvider = MixedRealityToolkit.GetService<ITeleportValidationProvider>());

        /// <inheritdoc />
        public ITeleportLocomotionProvider RequestingLocomotionProvider { get; private set; }

        /// <inheritdoc />
        public IMixedRealityInputSource InputSource => InputSourceParent;

        /// <inheritdoc />
        public MixedRealityPose? TargetPose { get; private set; }

        /// <inheritdoc />
        public ITeleportHotSpot HotSpot { get; private set; }

        /// <inheritdoc />
        public TeleportValidationResult ValidationResult { get; private set; } = TeleportValidationResult.None;

        protected Gradient GetLineGradient(TeleportValidationResult targetResult)
        {
            switch (targetResult)
            {
                case TeleportValidationResult.None:
                    return LineColorNoTarget;
                case TeleportValidationResult.Valid:
                    return LineColorValid;
                case TeleportValidationResult.Invalid:
                    return LineColorInvalid;
                case TeleportValidationResult.HotSpot:
                    return lineColorHotSpot;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetResult), targetResult, null);
            }
        }

        #region IMixedRealityPointer Implementation

        /// <inheritdoc />
        public override bool IsInteractionEnabled => !IsTeleportRequestActive && teleportEnabled;

        /// <inheritdoc />
        public override float PointerOrientation
        {
            get
            {
                if (HotSpot != null &&
                    HotSpot.OverrideTargetOrientation &&
                    ValidationResult == TeleportValidationResult.HotSpot)
                {
                    return HotSpot.TargetOrientation;
                }

                return base.PointerOrientation;
            }
            set => base.PointerOrientation = value;
        }

        public override void OnPreRaycast()
        {
            if (LineBase == null)
            {
                return;
            }

            // Make sure our array will hold
            if (Rays == null || Rays.Length != LineCastResolution)
            {
                Rays = new RayStep[LineCastResolution];
            }

            float stepSize = 1f / Rays.Length;
            Vector3 lastPoint = LineBase.GetUnClampedPoint(0f);

            for (int i = 0; i < Rays.Length; i++)
            {
                Vector3 currentPoint = LineBase.GetUnClampedPoint(stepSize * (i + 1));
                Rays[i].UpdateRayStep(ref lastPoint, ref currentPoint);
                lastPoint = currentPoint;
            }
        }

        public override void OnPostRaycast()
        {
            // Use the results from the last update to set our NavigationResult
            float clearWorldLength = 0f;
            ValidationResult = TeleportValidationResult.None;
            TargetPose = null;

            if (IsInteractionEnabled)
            {
                LineBase.enabled = true;

                // If we hit something
                if (Result.CurrentPointerTarget != null)
                {
                    // Check for hotspot hit.
                    HotSpot = Result.CurrentPointerTarget.GetComponent<ITeleportHotSpot>();

                    // Validate whether hit target is a valid teleportation target.
                    ValidationResult = ValidationDataProvider.IsValid(Result, HotSpot);

                    // Set target pose if we have a valid target.
                    if (ValidationResult == TeleportValidationResult.Valid ||
                        ValidationResult == TeleportValidationResult.HotSpot)
                    {
                        TargetPose = new MixedRealityPose(Result.EndPoint, Quaternion.Euler(0f, PointerOrientation, 0f));
                    }

                    // Use the step index to determine the length of the hit
                    for (int i = 0; i <= Result.RayStepIndex; i++)
                    {
                        if (i == Result.RayStepIndex)
                        {
                            if (MixedRealityRaycaster.DebugEnabled)
                            {
                                Color debugColor = ValidationResult != TeleportValidationResult.None
                                    ? Color.yellow
                                    : Color.cyan;

                                Debug.DrawLine(Result.StartPoint + Vector3.up * 0.1f, Result.StartPoint + Vector3.up * 0.1f, debugColor);
                            }

                            // Only add the distance between the start point and the hit
                            clearWorldLength += Vector3.Distance(Result.StartPoint, Result.EndPoint);
                        }
                        else if (i < Result.RayStepIndex)
                        {
                            // Add the full length of the step to our total distance
                            clearWorldLength += Rays[i].Length;
                        }
                    }

                    // Clamp the end of the parabola to the result hit's point
                    LineBase.LineEndClamp = LineBase.GetNormalizedLengthFromWorldLength(clearWorldLength, LineCastResolution);
                    BaseCursor?.SetVisibility(ValidationResult == TeleportValidationResult.Valid || ValidationResult == TeleportValidationResult.HotSpot);
                }
                else
                {
                    BaseCursor?.SetVisibility(false);
                    LineBase.LineEndClamp = 1f;
                }

                // Set the line color
                for (int i = 0; i < LineRenderers.Length; i++)
                {
                    LineRenderers[i].LineColor = GetLineGradient(ValidationResult);
                }
            }
            else
            {
                LineBase.enabled = false;
            }
        }

        #endregion IMixedRealityPointer Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<Vector2> eventData)
        {
            // Don't process input if we've got an active teleport request in progress.
            if (eventData.used || IsTeleportRequestActive || LocomotionSystem == null)
            {
                return;
            }

            if (eventData.SourceId == InputSourceParent.SourceId &&
                eventData.Handedness == Handedness &&
                eventData.MixedRealityInputAction == RequestingLocomotionProvider.InputAction)
            {
                eventData.Use();
                //ProcessDualAxisTeleportInput(eventData);
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        #region IMixedRealityTeleportHandler Implementation

        /// <inheritdoc />
        public override void OnTeleportTargetRequested(LocomotionEventData eventData)
        {
            // Only enable teleport if the request is addressed at our input source.
            if (eventData.EventSource.SourceId == InputSource.SourceId)
            {
                // This teleport target provider is able to provide a target
                // for the requested input source.
                ((ITeleportLocomotionProvider)eventData.LocomotionProvider).SetTargetProvider(this);

                teleportEnabled = true;
                IsTeleportRequestActive = false;
            }
        }

        /// <inheritdoc />
        public override void OnTeleportCompleted(LocomotionEventData eventData)
        {
            // We could be checking here whether the completed teleport
            // is this teleport provider's own teleport operation and act differently
            // depending on whether yes or not. But for now we'll make any teleport completion
            // basically cancel out any other teleport pointer as well.
            teleportEnabled = false;
            IsTeleportRequestActive = false;
            BaseCursor?.SetVisibility(false);
        }

        /// <inheritdoc />
        public override void OnTeleportCanceled(LocomotionEventData eventData)
        {
            // Only cancel teleport if this target provider's teleport was canceled.
            if (eventData.EventSource.SourceId == InputSource.SourceId)
            {
                teleportEnabled = false;
                IsTeleportRequestActive = false;
                BaseCursor?.SetVisibility(false);
            }
        }

        #endregion IMixedRealityTeleportHandler Implementation

        //private void ProcessDualAxisTeleportInput(InputEventData<Vector2> eventData)
        //{
        //    currentDualAxisInputPosition = eventData.InputData;

        //    if (Mathf.Abs(currentDualAxisInputPosition.y) > inputThreshold ||
        //        Mathf.Abs(currentDualAxisInputPosition.x) > inputThreshold)
        //    {
        //        // Get the angle of the pointer input
        //        float angle = Mathf.Atan2(currentDualAxisInputPosition.x, currentDualAxisInputPosition.y) * Mathf.Rad2Deg;

        //        // Offset the angle so it's 'forward' facing
        //        angle += angleOffset;
        //        PointerOrientation = angle;

        //        if (!teleportEnabled)
        //        {
        //            float absoluteAngle = Mathf.Abs(angle);

        //            if (absoluteAngle < teleportActivationAngle)
        //            {
        //                teleportEnabled = true;

        //                LocomotionSystem?.RaiseTeleportRequest(this, HotSpot);
        //            }
        //            else if (canMove)
        //            {
        //                // wrap the angle value.
        //                if (absoluteAngle > 180f)
        //                {
        //                    absoluteAngle = Mathf.Abs(absoluteAngle - 360f);
        //                }

        //                // Calculate the offset rotation angle from the 90 degree mark.
        //                // Half the rotation activation angle amount to make sure the activation angle stays centered at 90.
        //                float offsetRotationAngle = 90f - rotateActivationAngle;

        //                // subtract it from our current angle reading
        //                offsetRotationAngle = absoluteAngle - offsetRotationAngle;

        //                // if it's less than zero, then we don't have activation
        //                if (offsetRotationAngle > 0)
        //                {
        //                    var cameraRig = CameraSystem.MainCameraRig;

        //                    Debug.Assert(cameraRig != null, $"{nameof(TeleportPointer)} requires the {nameof(IMixedRealityCameraSystem)} be enabled with a valid {nameof(IMixedRealityCameraRig)}!");

        //                    // check to make sure we're still under our activation threshold.
        //                    if (offsetRotationAngle < rotateActivationAngle)
        //                    {
        //                        canMove = false;
        //                        // Rotate the camera by the rotation amount.  If our angle is positive then rotate in the positive direction, otherwise in the opposite direction.
        //                        cameraRig.PlayspaceTransform.RotateAround(cameraRig.CameraTransform.position, Vector3.up, angle >= 0.0f ? rotationAmount : -rotationAmount);
        //                    }
        //                    else // We may be trying to strafe backwards.
        //                    {
        //                        // Calculate the offset rotation angle from the 180 degree mark.
        //                        // Half the strafe activation angle to make sure the activation angle stays centered at 180f
        //                        float offsetStrafeAngle = 180f - backStrafeActivationAngle;
        //                        // subtract it from our current angle reading
        //                        offsetStrafeAngle = absoluteAngle - offsetStrafeAngle;

        //                        // Check to make sure we're still under our activation threshold.
        //                        if (offsetStrafeAngle > 0 && offsetStrafeAngle < backStrafeActivationAngle)
        //                        {
        //                            canMove = false;
        //                            var playspacePosition = cameraRig.PlayspaceTransform.position;
        //                            var height = playspacePosition.y;
        //                            var newPosition = -cameraRig.CameraTransform.forward * strafeAmount + playspacePosition;
        //                            newPosition.y = height;
        //                            cameraRig.PlayspaceTransform.position = newPosition;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (!canTeleport && !teleportEnabled)
        //        {
        //            // Reset the move flag when the user stops moving the joystick
        //            // but hasn't yet started teleport request.
        //            canMove = true;
        //        }

        //        if (canTeleport)
        //        {
        //            canTeleport = false;
        //            teleportEnabled = false;

        //            if (ValidationResult == TeleportValidationResult.Valid ||
        //                ValidationResult == TeleportValidationResult.HotSpot)
        //            {
        //                LocomotionSystem?.RaiseTeleportStarted(RequestingLocomotionProvider, this, HotSpot);
        //            }
        //        }

        //        if (teleportEnabled)
        //        {
        //            canTeleport = false;
        //            teleportEnabled = false;
        //            LocomotionSystem?.RaiseTeleportCanceled(RequestingLocomotionProvider, this, HotSpot);
        //        }
        //    }

        //    if (teleportEnabled &&
        //        ValidationResult == TeleportValidationResult.Valid ||
        //        ValidationResult == TeleportValidationResult.HotSpot)
        //    {
        //        canTeleport = true;
        //    }
        //}
    }
}
