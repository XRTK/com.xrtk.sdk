﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Devices;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Simulation.Hands;
using XRTK.Utilities;
using XRTK.Utilities.Physics;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// Default Mouse Pointer Implementation.
    /// </summary>
    public class MousePointer : BaseControllerPointer, IMixedRealityMousePointer
    {
        private float lastUpdateTime = 0.0f;

        private bool isInteractionEnabled = false;

        private bool cursorWasDisabledOnDown = false;

        private bool isDisabled = true;

        private Vector2 lastPosition;

        #region IMixedRealityMousePointer Implementaiton

        [SerializeField]
        [Tooltip("Should the mouse cursor be hidden when no active input is received?")]
        private bool hideCursorWhenInactive = true;

        /// <inheritdoc />
        bool IMixedRealityMousePointer.HideCursorWhenInactive => hideCursorWhenInactive;

        [SerializeField]
        [Range(0.01f, 1f)]
        [Tooltip("What is the movement threshold to reach before un-hiding mouse cursor?")]
        private float movementThresholdToUnHide = 0.1f;

        /// <inheritdoc />
        float IMixedRealityMousePointer.MovementThresholdToUnHide => movementThresholdToUnHide;

        [SerializeField]
        [Range(0f, 10f)]
        [Tooltip("How long should it take before the mouse cursor is hidden?")]
        private float hideTimeout = 3.0f;

        /// <inheritdoc />
        float IMixedRealityMousePointer.HideTimeout => hideTimeout;

        [SerializeField]
        [Range(0.1f, 1f)]
        [Tooltip("Mouse cursor speed that gets applied to the mouse delta.")]
        private float speed = 0.25f;

        float IMixedRealityMousePointer.Speed => speed;

        #endregion IMixedRealityMousePointer Implementation

        #region IMixedRealityPointer Implementaiton

        /// <inheritdoc />
        public override bool IsInteractionEnabled => isInteractionEnabled;

        private IMixedRealityController controller;

        /// <inheritdoc />
        public override IMixedRealityController Controller
        {
            get => controller;
            set
            {
                controller = value;
                InputSourceParent = value.InputSource;
                RaycastOrigin = CameraSystem != null
                    ? CameraSystem.MainCameraRig.CameraTransform
                    : CameraCache.Main.transform;
                Handedness = value.ControllerHandedness;
                gameObject.name = "Spatial Mouse Pointer";
                TrackingState = TrackingState.NotApplicable;
            }
        }

        /// <inheritdoc />
        public override bool TryGetPointingRay(out Ray pointingRay)
        {
            var playerCamera = CameraSystem != null
                ? CameraSystem.MainCameraRig.PlayerCamera
                : CameraCache.Main;
            pointingRay = playerCamera.ScreenPointToRay(UnityEngine.Input.mousePosition, Camera.MonoOrStereoscopicEye.Mono);
            return true;
        }

        /// <inheritdoc />
        public override bool TryGetPointerPosition(out Vector3 position)
        {
            position = transform.position;
            return true;
        }

        /// <inheritdoc />
        public override bool TryGetPointerRotation(out Quaternion rotation)
        {
            rotation = transform.rotation;
            return true;
        }

        /// <inheritdoc />
        public override void OnPreRaycast()
        {
            if (TryGetPointingRay(out var pointingRay))
            {
                Rays[0].CopyRay(pointingRay, PointerExtent);

                if (RayStabilizer != null)
                {
                    RayStabilizer.UpdateStability(Rays[0].Origin, Rays[0].Direction);
                    Rays[0].CopyRay(RayStabilizer.StableRay, PointerExtent);

                    if (MixedRealityRaycaster.DebugEnabled)
                    {
                        Debug.DrawRay(RayStabilizer.StableRay.origin, RayStabilizer.StableRay.direction * PointerExtent, Color.yellow);
                    }
                }
                else if (MixedRealityRaycaster.DebugEnabled)
                {
                    Debug.DrawRay(pointingRay.origin, pointingRay.direction * PointerExtent, Color.yellow);
                }
            }
        }

        /// <inheritdoc />
        public override void OnPostRaycast()
        {
            transform.position = Result.EndPoint;
            transform.LookAt(CameraSystem != null ? CameraSystem.MainCameraRig.CameraTransform : CameraCache.Main.transform);
        }

        #endregion IMixedRealityPointer Implementaiton

        #region IMixedRealitySourcePoseHandler Implementaiton

        /// <inheritdoc />
        public override void OnSourceDetected(SourceStateEventData eventData)
        {
            base.OnSourceDetected(eventData);

            if (eventData.SourceId == Controller?.InputSource.SourceId)
            {
                if (RayStabilizer != null)
                {
                    RayStabilizer = null;
                }

                isInteractionEnabled = true;
            }

            if (eventData.Controller is ISimulatedControllerDataProvider)
            {
                isInteractionEnabled = false;
                BaseCursor?.SetVisibility(false);
            }
        }

        /// <inheritdoc />
        public override void OnSourceLost(SourceStateEventData eventData)
        {
            base.OnSourceLost(eventData);

            if (eventData.SourceId == Controller?.InputSource.SourceId)
            {
                isInteractionEnabled = false;
            }

            if (eventData.Controller is ISimulatedControllerDataProvider)
            {
                isInteractionEnabled = true;
                BaseCursor?.SetVisibility(true);
            }
        }

        /// <inheritdoc />
        public override void OnSourcePoseChanged(SourcePoseEventData<Vector2> eventData)
        {
            if (Controller == null ||
                eventData.Controller == null ||
                eventData.Controller.InputSource.SourceId != Controller.InputSource.SourceId)
            {
                return;
            }

            if (UseSourcePoseData)
            {
                UpdateMousePosition(eventData.SourceData);
            }
        }

        #endregion IMixedRealitySourcePoseHandler Implementaiton

        #region IMixedRealityInputHandler Implementaiton

        /// <inheritdoc />
        public override void OnInputDown(InputEventData eventData)
        {
            if (eventData.SourceId == Controller?.InputSource.SourceId)
            {
                cursorWasDisabledOnDown = isDisabled;

                if (cursorWasDisabledOnDown)
                {
                    BaseCursor?.SetVisibility(true);
                    isDisabled = false;
                }
                else
                {
                    base.OnInputDown(eventData);
                }
            }
        }

        /// <inheritdoc />
        public override void OnInputUp(InputEventData eventData)
        {
            if (eventData.SourceId == Controller?.InputSource.SourceId)
            {
                if (!isDisabled && !cursorWasDisabledOnDown)
                {
                    base.OnInputUp(eventData);
                }
            }
        }

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<Vector2> eventData)
        {
            if (!isInteractionEnabled) { return; }

            if (eventData.SourceId == Controller?.InputSource.SourceId)
            {
                if (!UseSourcePoseData &&
                    PoseAction == eventData.InputAction)
                {
                    UpdateMousePosition(eventData.InputData);
                }
            }
        }

        #endregion IMixedRealityInputHandler Implementaiton

        #region Monobehaviour Implementaiton

        protected override void Start()
        {
            isDisabled = DisableCursorOnStart;

            base.Start();

            if (RayStabilizer != null)
            {
                RayStabilizer = null;
            }

            foreach (var inputSource in InputSystem.DetectedInputSources)
            {
                if (inputSource.SourceId == Controller.InputSource.SourceId)
                {
                    isInteractionEnabled = true;
                    break;
                }
            }
        }

        private void Update()
        {
            if (!hideCursorWhenInactive || isDisabled) { return; }

            if (Time.time - lastUpdateTime >= hideTimeout)
            {
                BaseCursor?.SetVisibility(false);
                isDisabled = true;
                lastUpdateTime = Time.time;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                isDisabled = true;
            }
        }

        #endregion Monobehaviour Implementaiton

        private void UpdateMousePosition(Vector2 mousePosition)
        {
            var shouldUpdate = false;
            var delta = lastPosition - mousePosition;
            lastPosition = mousePosition;
            var scaledMouseX = delta.x * speed;
            var scaledMouseY = delta.y * speed;

            if (Mathf.Abs(scaledMouseX) >= movementThresholdToUnHide ||
                Mathf.Abs(scaledMouseY) >= movementThresholdToUnHide)
            {
                if (isDisabled)
                {
                    BaseCursor?.SetVisibility(true);
                }

                shouldUpdate = true;
                isDisabled = false;
            }

            if (!isDisabled && shouldUpdate)
            {
                lastUpdateTime = Time.time;
            }
        }
    }
}
