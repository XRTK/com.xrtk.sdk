// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.SDK.UX;
using XRTK.Services;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// This input handler is designed to help facilitate the physical manipulation of <see cref="GameObject"/>s across all platforms.
    /// Users will be able to use the select action to activate the manipulation phase, then various gestures and supplemental actions to
    /// nudge, rotate, and scale the object.
    /// </summary>
    public class ManipulationHandler : BaseInputHandler,
        IMixedRealityPointerHandler,
        IMixedRealityInputHandler,
        IMixedRealityInputHandler<float>,
        IMixedRealityInputHandler<Vector2>
    {
        private const int IgnoreRaycastLayer = 2;

        #region Input Actions

        [Header("Input Actions")]

        [SerializeField]
        [Tooltip("The action to use to select the GameObject and begin/end the manipulation phase")]
        private MixedRealityInputAction selectAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to select the GameObject and begin/end the manipulation phase
        /// </summary>
        public MixedRealityInputAction SelectAction
        {
            get => selectAction;
            set => selectAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to enable pressed actions")]
        private MixedRealityInputAction touchpadPressAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to enable pressed actions
        /// </summary>
        public MixedRealityInputAction TouchpadPressAction
        {
            get => touchpadPressAction;
            set => touchpadPressAction = value;
        }

        [Range(0f, 1f)]
        [SerializeField]
        private float pressThreshold = 0.25f;

        public float PressThreshold
        {
            get => pressThreshold;
            set => pressThreshold = value;
        }

        [SerializeField]
        [Tooltip("The action to use to rotate the GameObject")]
        private MixedRealityInputAction rotateAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to rotate the GameObject
        /// </summary>
        public MixedRealityInputAction RotateAction
        {
            get => rotateAction;
            set => rotateAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to scale the GameObject")]
        private MixedRealityInputAction scaleAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to scale the GameObject
        /// </summary>
        public MixedRealityInputAction ScaleAction
        {
            get => scaleAction;
            set => scaleAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to nudge the pointer extent closer or further away from the pointer source")]
        private MixedRealityInputAction nudgeAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to nudge the <see cref="GameObject"/> closer or further away from the pointer source.
        /// </summary>
        public MixedRealityInputAction NudgeAction
        {
            get => nudgeAction;
            set => nudgeAction = value;
        }

        [SerializeField]
        [Tooltip("The action to use to immediately cancel any current manipulation.")]
        private MixedRealityInputAction cancelAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to immediately cancel any current manipulation.
        /// </summary>
        public MixedRealityInputAction CancelAction
        {
            get => cancelAction;
            set => cancelAction = value;
        }

        #endregion Input Actions

        #region Manipulation Options

        [Header("Options")]

        [SerializeField]
        [Tooltip("The object to manipulate using this handler. Automatically uses this transform if none is set.")]
        private Transform manipulationTarget;

        [SerializeField]
        [Tooltip("The spatial mesh visibility while manipulating an object.")]
        private SpatialMeshDisplayOptions spatialMeshVisibility = SpatialMeshDisplayOptions.Visible;

        /// <summary>
        /// The spatial mesh visibility while manipulating an object.
        /// </summary>
        public SpatialMeshDisplayOptions SpatialMeshVisibility
        {
            get => spatialMeshVisibility;
            set => spatialMeshVisibility = value;
        }

        [SerializeField]
        [Tooltip("Should the user press and hold the select action or press to hold and press again to release?")]
        private bool useHold = true;

        /// <summary>
        /// Should the user press and hold the select action or press to hold and press again to release?
        /// </summary>
        public bool UseHold
        {
            get => useHold;
            set => useHold = value;
        }

        [SerializeField]
        private HapticFeedbackType grabHapticFeedback = HapticFeedbackType.ForceDown;

        [SerializeField]
        private HapticFeedbackType releaseHapticFeedback = HapticFeedbackType.ForceUp;

        #region Scale Options

        [SerializeField]
        [Tooltip("The min/max values to activate the scale action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 scaleZone = new Vector2(0.25f, 1f);

        /// <summary>
        /// The dual axis zone to process and activate the scale action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 ScaleZone
        {
            get => scaleZone;
            set => scaleZone = value;
        }

        [SerializeField]
        [Tooltip("The amount to scale the GameObject")]
        private float scaleAmount = 0.5f;

        /// <summary>
        /// The amount to scale the <see cref="GameObject"/>
        /// </summary>
        public float ScaleAmount
        {
            get => scaleAmount;
            set => scaleAmount = value;
        }

        [SerializeField]
        [Tooltip("The min and max size this object can be scaled to.")]
        private Vector2 scaleConstraints = new Vector2(0.02f, 2f);

        /// <summary>
        /// The min and max size this object can be scaled to.
        /// </summary>
        public Vector2 ScaleConstraints
        {
            get => scaleConstraints;
            set => scaleConstraints = value;
        }

        #endregion Scale Options

        #region Rotation Options

        [SerializeField]
        [Tooltip("The min/max values to activate the rotate action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 rotationZone = new Vector2(0.25f, 1f);

        /// <summary>
        /// The dual axis zone to process and activate the scale action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 RotationZone
        {
            get => rotationZone;
            set => rotationZone = value;
        }

        [SerializeField]
        [Range(2.8125f, 45f)]
        [Tooltip("The amount a user has to scroll in a circular motion to activate the rotation action.")]
        private float rotationAngleActivation = 11.25f;

        /// <summary>
        /// The amount a user has to scroll in a circular motion to activate the rotation action.
        /// </summary>
        public float RotationAngleActivation
        {
            get => rotationAngleActivation;
            set => rotationAngleActivation = value;
        }

        #endregion Rotation Options

        #region Nudge Options

        [SerializeField]
        [Tooltip("The min/max values to activate the nudge action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 nudgeZone = new Vector2(0.25f, 1f);

        /// <summary>
        /// The dual axis zone to process and activate the nudge action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 NudgeZone
        {
            get => nudgeZone;
            set => nudgeZone = value;
        }

        [SerializeField]
        [Range(0.1f, 0.001f)]
        [Tooltip("The amount to nudge the position of the GameObject")]
        private float nudgeAmount = 0.01f;

        /// <summary>
        /// The amount to nudge the position of the <see cref="GameObject"/>
        /// </summary>
        public float NudgeAmount
        {
            get => nudgeAmount;
            set => nudgeAmount = value;
        }

        [SerializeField]
        [Tooltip("The min and max values for the nudge amount.")]
        private Vector2 nudgeConstraints = new Vector2(0.25f, 10f);

        /// <summary>
        /// The min and max values for the nudge amount.
        /// </summary>
        public Vector2 NudgeConstraints
        {
            get => nudgeConstraints;
            set => nudgeConstraints = value;
        }

        #endregion Nudge Options

        #endregion Manipulation Options

        #region Properties

        /// <summary>
        /// The current status of the hold.
        /// </summary>
        /// <remarks>
        /// Used to determine if the <see cref="GameObject"/> is currently being manipulated by the user.
        /// </remarks>
        public bool IsBeingHeld { get; private set; } = false;

        /// <summary>
        /// Is the <see cref="primaryInputSource"/> currently pressed?
        /// </summary>
        public bool IsPressed { get; private set; } = false;

        /// <summary>
        /// Is there currently a manipulation processing a rotation?
        /// </summary>
        public bool IsRotating { get; private set; } = false;

        /// <summary>
        /// Is scaling possible?
        /// </summary>
        public bool IsScalingPossible { get; private set; } = false;

        /// <summary>
        /// Is nudge possible?
        /// </summary>
        public bool IsNudgePossible { get; private set; } = false;

        /// <summary>
        /// Is rotation possible?
        /// </summary>
        public bool IsRotationPossible { get; private set; } = false;

        #endregion Properties

        private BoundingBox boundingBox;

        private IMixedRealityPointer primaryPointer;

        private IMixedRealityInputSource primaryInputSource;

        private int prevPhysicsLayer;
        private int boundingBoxPrevPhysicsLayer;
        private SpatialMeshDisplayOptions prevSpatialMeshDisplay;

        private float updatedExtent;
        private float prevPointerExtent;

        private Vector2 lastPositionReading;

        private Vector3 prevScale;
        private Vector3 prevPosition;
        private Vector3 updatedScale;
        private Vector3 grabbedPosition;

        private Quaternion prevRotation;

        #region Monobehaviour Implementation

        protected virtual void Awake()
        {
            if (manipulationTarget == null)
            {
                manipulationTarget = transform;
            }

            boundingBox = GetComponent<BoundingBox>();
        }

        protected virtual void Update()
        {
            if (!IsBeingHeld || primaryPointer == null) { return; }

            var pointerPosition = primaryPointer.Result.EndPoint;

            if (!IsPressed)
            {
                if (!IsRotating && !IsScalingPossible)
                {
                    manipulationTarget.position = grabbedPosition + pointerPosition;
                }
            }
            else
            {
                if (IsNudgePossible)
                {
                    manipulationTarget.position = grabbedPosition + pointerPosition;
                    primaryPointer.PointerExtent = updatedExtent;
                }
                else if (IsScalingPossible)
                {
                    manipulationTarget.position = grabbedPosition + pointerPosition;
                    manipulationTarget.ScaleAround(pointerPosition, updatedScale);

                    if (prevPosition != Vector3.zero)
                    {
                        grabbedPosition = manipulationTarget.position - pointerPosition;
                    }
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (IsBeingHeld)
            {
                // We don't pass IsCancelled here because
                // it's the intended behaviour to end the hold
                // if the component is disabled.
                EndHold();
            }
        }

        #endregion Monobehaviour Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData)
        {
            if (!eventData.used &&
                IsBeingHeld &&
                eventData.MixedRealityInputAction == cancelAction)
            {
                EndHold(true);
                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputUp(InputEventData eventData)
        {
            if (!eventData.used &&
                IsBeingHeld &&
                eventData.MixedRealityInputAction == cancelAction)
            {
                EndHold(true);
                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<float> eventData)
        {
            if (!IsBeingHeld ||
                primaryInputSource == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

            if (eventData.MixedRealityInputAction == touchpadPressAction &&
                eventData.InputData <= 0.00001f)
            {
                IsRotating = false;
                IsNudgePossible = false;
                IsScalingPossible = false;
                lastPositionReading.x = 0f;
                lastPositionReading.y = 0f;
                eventData.Use();
            }

            if (IsRotating) { return; }

            if (!IsPressed &&
                eventData.MixedRealityInputAction == touchpadPressAction &&
                eventData.InputData >= pressThreshold)
            {
                IsPressed = true;
                eventData.Use();
            }

            if (IsPressed &&
                eventData.MixedRealityInputAction == touchpadPressAction &&
                eventData.InputData <= pressThreshold)
            {
                IsPressed = false;
                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData)
        {
            // reset this in case we are rotating only.
            IsScalingPossible = false;

            if (!IsBeingHeld ||
                primaryInputSource == null ||
                primaryPointer == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

            var pointerPosition = primaryPointer.Result.EndPoint;

            // Filter our actions
            if (eventData.MixedRealityInputAction != nudgeAction ||
                eventData.MixedRealityInputAction != scaleAction ||
                eventData.MixedRealityInputAction != rotateAction)
            {
                return;
            }

            var absoluteInputData = eventData.InputData;
            absoluteInputData.x = Mathf.Abs(absoluteInputData.x);
            absoluteInputData.y = Mathf.Abs(absoluteInputData.y);

            IsRotationPossible = eventData.MixedRealityInputAction == rotateAction &&
                                 (absoluteInputData.x >= rotationZone.x ||
                                  absoluteInputData.y >= rotationZone.x);

            if (!IsPressed &&
                IsRotationPossible &&
                !lastPositionReading.x.Equals(0f) && !lastPositionReading.y.Equals(0f))
            {
                var rotationAngle = Vector2.SignedAngle(lastPositionReading, eventData.InputData);

                if (Mathf.Abs(rotationAngle) > rotationAngleActivation)
                {
                    IsRotating = true;
                }

                if (IsRotating)
                {
                    manipulationTarget.position = grabbedPosition + pointerPosition;
                    manipulationTarget.RotateAround(pointerPosition, Vector3.up, -rotationAngle);

                    if (prevPosition != Vector3.zero)
                    {
                        grabbedPosition = manipulationTarget.position - pointerPosition;
                    }

                    eventData.Use();
                }
            }

            lastPositionReading = eventData.InputData;

            if (!IsPressed || IsRotating) { return; }

            IsScalingPossible = eventData.MixedRealityInputAction == scaleAction && absoluteInputData.x > 0f;
            IsNudgePossible = eventData.MixedRealityInputAction == nudgeAction && absoluteInputData.y > 0f;

            // Check to make sure that input values fall between min/max zone values
            if (IsScalingPossible &&
                (absoluteInputData.x <= scaleZone.x ||
                 absoluteInputData.x >= scaleZone.y))
            {
                IsScalingPossible = false;
            }

            // Check to make sure that input values fall between min/max zone values
            if (IsNudgePossible &&
                (absoluteInputData.y <= nudgeZone.x ||
                 absoluteInputData.y >= nudgeZone.y))
            {
                IsNudgePossible = false;
            }

            // Disable any actions if min zone values overlap.
            if (absoluteInputData.x <= scaleZone.x &&
                absoluteInputData.y <= nudgeZone.x)
            {
                IsNudgePossible = false;
                IsScalingPossible = false;
            }

            if (IsScalingPossible && IsNudgePossible)
            {
                IsNudgePossible = false;
                IsScalingPossible = false;
            }

            if (IsNudgePossible)
            {
                Debug.Assert(primaryPointer != null);
                var newExtent = primaryPointer.PointerExtent;
                var currentRaycastDistance = primaryPointer.Result.RayDistance;

                // Reset the cursor extent to the nearest value in case we're hitting something close
                // and the user wants to adjust. That way it doesn't take forever to see the change.
                if (currentRaycastDistance < newExtent)
                {
                    newExtent = currentRaycastDistance;
                }

                var prevExtent = newExtent;
                newExtent = CalculateNudgeDistance(eventData, prevExtent);

                // check constraints
                if (eventData.InputData.y < 0f)
                {
                    if (newExtent <= nudgeConstraints.x)
                    {
                        newExtent = prevExtent;
                    }
                }
                else
                {
                    if (newExtent >= nudgeConstraints.y)
                    {
                        newExtent = prevExtent;
                    }
                }

                updatedExtent = newExtent;
                eventData.Use();
            }

            if (IsScalingPossible)
            {
                var newScale = manipulationTarget.localScale;
                var currentScale = newScale;
                newScale = CalculateScaleAmount(eventData, newScale);

                // We can check any axis, they should all be the same as we do uniform scales.
                if (eventData.InputData.x < 0f && newScale.x <= scaleConstraints.x)
                {
                    newScale = currentScale;
                }
                else if (newScale.y >= scaleConstraints.y)
                {
                    newScale = currentScale;
                }

                updatedScale = newScale;

                eventData.Use();
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        #region IMixedRealityPointerHandler Implementation

        /// <inheritdoc />
        public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                if (!useHold)
                {
                    BeginHold(eventData);
                }

                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (eventData.used ||
                eventData.MixedRealityInputAction != selectAction)
            {
                return;
            }

            if (useHold)
            {
                if (IsBeingHeld)
                {
                    EndHold();
                }
                else
                {
                    BeginHold(eventData);
                }
            }

            if (!useHold && IsBeingHeld)
            {
                EndHold();
            }

            eventData.Use();
        }

        /// <inheritdoc />
        public virtual void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        #endregion IMixedRealityPointerHandler Implementation

        /// <summary>
        /// Begin a new hold on the manipulation target.
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void BeginHold(MixedRealityPointerEventData eventData)
        {
            if (IsBeingHeld) { return; }

            IsBeingHeld = true;

            if (primaryInputSource == null)
            {
                primaryInputSource = eventData.InputSource;
            }

            if (primaryPointer == null)
            {
                primaryPointer = eventData.Pointer;
            }

            primaryPointer.Controller?.SendHapticFeedback(grabHapticFeedback, 10f);
            MixedRealityToolkit.InputSystem.PushModalInputHandler(gameObject);

            if (MixedRealityToolkit.SpatialAwarenessSystem != null)
            {
                prevSpatialMeshDisplay = MixedRealityToolkit.SpatialAwarenessSystem.SpatialMeshVisibility;
                MixedRealityToolkit.SpatialAwarenessSystem.SpatialMeshVisibility = spatialMeshVisibility;
            }

            var pointerPosition = primaryPointer.Result.EndPoint;

            prevPosition = manipulationTarget.position;

            if (prevPosition != Vector3.zero)
            {
                grabbedPosition = prevPosition - pointerPosition;

                prevPointerExtent = primaryPointer.PointerExtent;
                // update the pointer extent to prevent the object from popping to the end of the pointer
                var currentRaycastDistance = primaryPointer.Result.RayDistance;
                primaryPointer.PointerExtent = currentRaycastDistance;
            }

            prevScale = manipulationTarget.localScale;
            prevRotation = manipulationTarget.rotation;

            prevPhysicsLayer = manipulationTarget.gameObject.layer;
            manipulationTarget.SetLayerRecursively(IgnoreRaycastLayer);

            if (boundingBox != null)
            {
                boundingBoxPrevPhysicsLayer = boundingBox.gameObject.layer;
                boundingBox.transform.SetLayerRecursively(IgnoreRaycastLayer);
            }

            eventData.Use();
        }

        /// <summary>
        /// Ends the current hold on the manipulation target.
        /// </summary>
        /// <param name="isCanceled">
        /// Is this a cancelled action? If so then the manipulation target will pop back into its prev position.
        /// </param>
        public virtual void EndHold(bool isCanceled = false)
        {
            if (!IsBeingHeld) { return; }

            if (MixedRealityToolkit.SpatialAwarenessSystem != null)
            {
                MixedRealityToolkit.SpatialAwarenessSystem.SpatialMeshVisibility = prevSpatialMeshDisplay;
            }

            primaryPointer?.Controller?.SendHapticFeedback(releaseHapticFeedback, 10f);
            primaryPointer.PointerExtent = prevPointerExtent;

            if (isCanceled)
            {
                manipulationTarget.position = prevPosition;
                manipulationTarget.localScale = prevScale;
                manipulationTarget.rotation = prevRotation;
            }

            MixedRealityToolkit.InputSystem.PopModalInputHandler();

            manipulationTarget.SetLayerRecursively(prevPhysicsLayer);

            if (boundingBox != null)
            {
                boundingBox.transform.SetLayerRecursively(boundingBoxPrevPhysicsLayer);
            }

            primaryPointer = null;
            primaryInputSource = null;
            IsBeingHeld = false;
        }

        /// <summary>
        /// Calculates the extent of the nudge using input event data.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="prevExtent">The previous extent distance of the pointer and raycast.</param>
        /// <returns>The new pointer extent.</returns>
        protected virtual float CalculateNudgeDistance(InputEventData<Vector2> eventData, float prevExtent)
        {
            return prevExtent + nudgeAmount * (eventData.InputData.y < 0f ? -1 : 1);
        }

        /// <summary>
        /// Calculates the scale amount using the input event data.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="scale">The previous scale</param>
        /// <returns>The new scale value.</returns>
        protected virtual Vector3 CalculateScaleAmount(InputEventData<Vector2> eventData, Vector3 scale)
        {
            return eventData.InputData.x < 0f ? scale * scaleAmount : scale / scaleAmount;
        }
    }
}
