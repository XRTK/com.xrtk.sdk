// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Physics;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.SpatialAwarenessSystem;
using XRTK.SDK.UX;
using XRTK.Services;
using XRTK.Utilities;
using XRTK.Utilities.Physics;

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

        public Transform ManipulationTarget
        {
            get => manipulationTarget;
            set
            {
                if (IsBeingHeld)
                {
                    Debug.LogWarning("Cannot set manipulation target while being held!");
                }
                else
                {
                    manipulationTarget = value;
                }
            }
        }

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
        [Tooltip("When the user grabs the object we used the position the pointer is at to perform all manipulations.\n\nWhen false this will only perform manipulations at the object's origin or by the provided offset in BeginHold")]
        private bool useGrabOffset = true;

        /// <summary>
        /// When the user grabs the object we used the position the pointer is at to perform all manipulations.
        /// </summary>
        /// <remarks>
        /// When false this will only perform manipulations at the object's origin or by the provided offset in <see cref="BeginHold"/>
        /// </remarks>
        public bool UseGrabOffset
        {
            get => useGrabOffset;
            set => useGrabOffset = value;
        }

        [SerializeField]
        [Tooltip("Smooths the motion of the object to the goal position.")]
        private bool smoothMotion = true;

        /// <summary>
        /// Smooths the motion of the object to the goal position.
        /// </summary>
        public bool SmoothMotion
        {
            get => smoothMotion;
            set => smoothMotion = value;
        }

        [SerializeField]
        private float smoothingFactor = 10f;

        /// <summary>
        /// When using <see cref="SmoothMotion"/>, this value helps tweak movement sensitivity to the goal position.
        /// </summary>
        public float SmoothingFactor
        {
            get => smoothingFactor;
            set => smoothingFactor = value;
        }

        [SerializeField]
        [Tooltip("Should the GameObject snap to valid surfaces?")]
        private bool snapToValidSurfaces = true;

        /// <summary>
        /// Should the <see cref="GameObject"/> snap to valid surfaces?
        /// </summary>
        public bool SnapToValidSurfaces
        {
            get => snapToValidSurfaces;
            set => snapToValidSurfaces = value;
        }

        [SerializeField]
        [Tooltip("This distance that will make the object snap to the surface.")]
        private float snapDistance = 0.0762f;

        /// <summary>
        /// This distance that will make the object snap to the surface.
        /// </summary>
        public float SnapDistance
        {
            get => snapDistance;
            set => snapDistance = value;
        }

        [SerializeField]
        [Tooltip("The distance that will make the object unsnap from the surface.")]
        private float unsnapTolerance = 1f;

        /// <summary>
        /// The distance that will make the object unsnap from the surface.
        /// </summary>
        public float UnsnapDistance
        {
            get => unsnapTolerance;
            set => unsnapTolerance = value;
        }

        [SerializeField]
        [Tooltip("Enables or disables translations for this handler.")]
        private bool isTranslateLocked = false;

        /// <summary>
        /// Enables or disables translations for this handler.
        /// </summary>
        public bool IsTranslateLocked
        {
            get => isTranslateLocked;
            set => isTranslateLocked = value;
        }

        #region Scale Options

        [SerializeField]
        [Tooltip("Enables or disabled scaling for this handler.")]
        private bool isScalingLocked = false;

        /// <summary>
        /// Enables or disabled scaling for this handler.
        /// </summary>
        public bool IsScalingLocked
        {
            get => isScalingLocked;
            set => isScalingLocked = value;
        }

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
        [Tooltip("Enables or disabled rotations for this handler.")]
        private bool isRotationLocked = false;

        /// <summary>
        /// Enables or disabled rotations for this handler.
        /// </summary>
        public bool IsRotationLocked
        {
            get => isRotationLocked;
            set => isRotationLocked = value;
        }

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
        private float rotationAngleActivation = 2.8125f;

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
        public bool IsRotating => !updatedAngle.Equals(0f);

        private bool isScalingPossible = false;

        /// <summary>
        /// Is scaling possible?
        /// </summary>
        public bool IsScalingPossible
        {
            get => isScalingPossible && !isScalingLocked;
            private set => isScalingPossible = value;
        }

        private bool isNudgePossible = false;

        /// <summary>
        /// Is nudge possible?
        /// </summary>
        public bool IsNudgePossible
        {
            get => isNudgePossible && !isTranslateLocked;
            private set => isNudgePossible = value;
        }

        private bool isRotationPossible = false;

        /// <summary>
        /// Is rotation possible?
        /// </summary>
        public bool IsRotationPossible
        {
            get => isRotationPossible && !isRotationLocked;
            private set => isRotationPossible = value;
        }

        /// <summary>
        /// Is the <see cref="GameObject"/> currently snapped to a surface?
        /// </summary>
        public virtual bool IsSnappedToSurface { get; private set; } = false;

        private Collider thisCollider;

        /// <summary>
        /// The <see cref="Collider"/> associated with this <see cref="GameObject"/>.
        /// </summary>
        public Collider Collider
        {
            get
            {
                var isSet = false;

                if (BoundingBox != null &&
                    BoundingBox.BoundingBoxCollider != null)
                {
                    thisCollider = BoundingBox.BoundingBoxCollider;
                    isSet = true;
                }

                if (thisCollider == null)
                {
                    thisCollider = gameObject.GetComponent<Collider>();
                    isSet = true;
                }

                if (thisCollider == null)
                {
                    thisCollider = gameObject.EnsureComponent<BoxCollider>();
                    isSet = true;
                }

                if (isSet)
                {
                    transform.GetColliderBounds(ref cachedColliders);
                }

                return thisCollider;
            }
        }

        /// <summary>
        /// The captured primary pointer for the current active hold.
        /// </summary>
        public IMixedRealityPointer PrimaryPointer { get; private set; }

        /// <summary>
        /// The <see cref="BoundingBox"/> associated to this <see cref="GameObject"/>.
        /// </summary>
        public BoundingBox BoundingBox { get; private set; }

        /// <summary>
        /// The current valid layer mask for any associated pointers when interacting with this object.
        /// </summary>
        protected LayerMask[] LayerMasks => PrimaryPointer?.PointerRaycastLayerMasksOverride ?? FocusProvider.GlobalPointerRaycastLayerMasks;

        #endregion Properties

        #region Events

        /// <summary>
        /// Invoked when a hold has begun.
        /// </summary>
        public event Action OnHoldBegin;

        /// <summary>
        /// Invoked when a hold has ended.
        /// </summary>
        public event Action<bool> OnHoldEnd;

        #endregion Events

        private IMixedRealityInputSource primaryInputSource;

        private SpatialMeshDisplayOptions prevSpatialMeshDisplay;

        private float updatedAngle;
        private float updatedExtent;
        private float prevPointerExtent;

        private Vector2 lastPositionReading;

        private Vector3 prevScale;
        private Vector3 prevPosition;
        private Vector3 updatedScale;

        private Quaternion prevRotation;

        private Rigidbody body;

        private float snappedVerticalPosition;

        private Transform snapTarget;
        private Collider[] cachedColliders;

        #region Monobehaviour Implementation

        protected virtual void Awake()
        {
            if (manipulationTarget == null)
            {
                manipulationTarget = transform;
            }

            body = gameObject.EnsureComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        protected override void Start()
        {
            base.Start();

            BoundingBox = GetComponent<BoundingBox>();
        }

        protected virtual void LateUpdate()
        {
            // only process the transform data if we don't have
            // a bounding box component attached, otherwise the
            // bounding box will call this method for us.
            if (BoundingBox == null)
            {
                ProcessTransformData();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (IsBeingHeld)
            {
                // Only flag cancelled if the application is quitting.
                EndHold(MixedRealityToolkit.IsApplicationQuitting);
            }
        }

        #endregion Monobehaviour Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == MixedRealityInputAction.None) { return; }

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
            if (eventData.MixedRealityInputAction == MixedRealityInputAction.None) { return; }

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
            if (eventData.MixedRealityInputAction == MixedRealityInputAction.None) { return; }

            if (!IsBeingHeld ||
                primaryInputSource == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

            if (eventData.MixedRealityInputAction == touchpadPressAction)
            {
                if (eventData.InputData <= 0.00001f)
                {
                    IsNudgePossible = false;
                    IsScalingPossible = false;
                    lastPositionReading.x = 0f;
                    lastPositionReading.y = 0f;
                    eventData.Use();
                }
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
            if (eventData.MixedRealityInputAction == MixedRealityInputAction.None) { return; }

            // reset this in case we are rotating only.
            IsScalingPossible = false;

            if (!IsBeingHeld ||
                primaryInputSource == null ||
                PrimaryPointer == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

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
                var angle = updatedAngle + CalculateRotationAngle(eventData.InputData, lastPositionReading);

                if (Mathf.Abs(angle) > rotationAngleActivation)
                {
                    updatedAngle = angle;
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
                Debug.Assert(PrimaryPointer != null);

                var nudgeFactor = eventData.InputData.y;
                var prevExtent = GetCurrentExtent(PrimaryPointer);
                var newExtent = CalculateNudgeDistance(nudgeFactor, prevExtent);
                updatedExtent = ClampExtent(nudgeFactor, newExtent, prevExtent);
                eventData.Use();
            }

            if (IsScalingPossible)
            {
                var scaleFactor = eventData.InputData.x;
                var newScale = CalculateScaleAmount(scaleFactor, manipulationTarget.localScale);
                updatedScale = ClampScale(scaleFactor, newScale);
                eventData.Use();
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        #region IMixedRealityPointerHandler Implementation

        /// <inheritdoc />
        public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == MixedRealityInputAction.None) { return; }

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
            if (eventData.MixedRealityInputAction == MixedRealityInputAction.None) { return; }

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
        /// <param name="grabOffset"></param>
        public virtual void BeginHold(MixedRealityPointerEventData eventData, Vector3? grabOffset = null)
        {
            if (IsBeingHeld) { return; }

            IsBeingHeld = true;

            if (primaryInputSource == null)
            {
                primaryInputSource = eventData.InputSource;
            }

            if (PrimaryPointer == null)
            {
                PrimaryPointer = eventData.Pointer;
            }

            InputSystem?.PushModalInputHandler(gameObject);

            if (MixedRealityToolkit.TryGetSystem<IMixedRealitySpatialAwarenessSystem>(out var spatialAwarenessSystem))
            {
                prevSpatialMeshDisplay = spatialAwarenessSystem.SpatialMeshVisibility;
                spatialAwarenessSystem.SpatialMeshVisibility = spatialMeshVisibility;
            }

            prevPosition = manipulationTarget.position;

            if (prevPosition == Vector3.zero)
            {
                manipulationTarget.position = PrimaryPointer.Result.EndPoint;
            }
            else
            {
                // update the pointer extent to prevent the object from popping to the end of the pointer
                prevPointerExtent = PrimaryPointer.PointerExtent;
                var currentRaycastDistance = PrimaryPointer.Result.RayDistance;
                PrimaryPointer.PointerExtent = currentRaycastDistance;
            }

            prevScale = manipulationTarget.localScale;
            prevRotation = manipulationTarget.rotation;

            PrimaryPointer.IsFocusLocked = true;
            PrimaryPointer.SyncedTarget = gameObject;

            // If the grab offset is not provided
            // and grab offset has been disabled
            // grab the object at the local origin
            if (!useGrabOffset && grabOffset == null)
            {
                grabOffset = Vector3.zero;
            }

            PrimaryPointer.OverrideGrabPoint = grabOffset;
            transform.SetCollidersActive(false, ref cachedColliders);
            Collider.enabled = true;
            body.isKinematic = false;

            eventData.Use();

            OnHoldBegin?.Invoke();
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

            if (MixedRealityToolkit.TryGetSystem<IMixedRealitySpatialAwarenessSystem>(out var spatialAwarenessSystem))
            {
                spatialAwarenessSystem.SpatialMeshVisibility = prevSpatialMeshDisplay;
            }

            if (prevPosition != Vector3.zero)
            {
                PrimaryPointer.PointerExtent = prevPointerExtent;
            }

            if (isCanceled)
            {
                manipulationTarget.position = prevPosition;
                manipulationTarget.localScale = prevScale;
                manipulationTarget.rotation = prevRotation;
            }

            InputSystem?.PopModalInputHandler();

            body.isKinematic = true;

            transform.SetCollidersActive(true, ref cachedColliders);

            PrimaryPointer.SyncedTarget = null;
            PrimaryPointer.OverrideGrabPoint = null;
            PrimaryPointer.IsFocusLocked = false;
            PrimaryPointer = null;
            primaryInputSource = null;
            IsBeingHeld = false;

            OnHoldEnd?.Invoke(isCanceled);
        }

        /// <summary>
        /// Calculates the extent of the nudge using input event data.
        /// </summary>
        /// <param name="nudgeFactor">The nudge factor.</param>
        /// <param name="prevExtent">The previous extent distance of the pointer and raycast.</param>
        /// <returns>The new pointer extent.</returns>
        protected virtual float CalculateNudgeDistance(float nudgeFactor, float prevExtent)
        {
            return prevExtent + nudgeAmount * (nudgeFactor < 0f ? -1 : 1);
        }

        private static float GetCurrentExtent(IMixedRealityPointer pointer)
        {
            var extent = pointer.PointerExtent;
            var currentRaycastDistance = pointer.Result.RayDistance;

            // Reset the cursor extent to the nearest value in case we're hitting something close
            // and the user wants to adjust. That way it doesn't take forever to see the change.
            if (currentRaycastDistance < extent)
            {
                extent = currentRaycastDistance;
            }

            return extent;
        }

        private float ClampExtent(float nudgeFactor, float newExtent, float prevExtent)
        {
            if (nudgeFactor < 0f)
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

            return newExtent;
        }

        /// <summary>
        /// Calculates the scale amount using the input event data.
        /// </summary>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <param name="scale">The previous scale</param>
        /// <returns>The new scale value.</returns>
        protected virtual Vector3 CalculateScaleAmount(float scaleFactor, Vector3 scale)
        {
            return scaleFactor < 0f ? scale * scaleAmount : scale / scaleAmount;
        }

        private Vector3 ClampScale(float scaleFactor, Vector3 scale)
        {
            // We can check any axis, they should all be the same
            // as we currently do uniform scales only.
            if (scaleFactor < 0f && scale.x <= scaleConstraints.x)
            {
                scale = manipulationTarget.localScale;
            }
            else if (scale.y >= scaleConstraints.y)
            {
                scale = manipulationTarget.localScale;
            }

            return scale;
        }

        /// <summary>
        /// Calculates the rotation angle using the input event data and the previous reading.
        /// </summary>
        /// <param name="currentReading">The current input reading.</param>
        /// <param name="previousReading">The input reading from the last event.</param>
        /// <returns>The new rotation angle.</returns>
        protected virtual float CalculateRotationAngle(Vector2 currentReading, Vector2 previousReading)
        {
            return Vector2.SignedAngle(previousReading, currentReading);
        }

        /// <summary>
        /// Do stuff when snap occurs.
        /// </summary>
        protected virtual void OnSnap() { }

        /// <summary>
        /// Do stuff when snap stops.
        /// </summary>
        protected virtual void OnUnsnap() { }

        /// <summary>
        /// Process the manipulation handler's pending transform updates.
        /// </summary>
        /// <remarks>
        /// This is called from the <see cref="BoundingBox"/>'s LateUpdate to properly sync the transforms to prevent
        /// jerky or stuttering effects when moving the objects. This can happen because of the non-deterministic way
        /// unity calls it's game loop events on scene objects.
        /// </remarks>
        public virtual void ProcessTransformData()
        {
            if (!IsBeingHeld ||
                PrimaryPointer == null ||
                PrimaryPointer.Result.LastHitObject == gameObject)
            {
                return;
            }

            Debug.Assert(PrimaryPointer.IsFocusLocked);
            Debug.Assert(PrimaryPointer.SyncedTarget != null);

            var pointerPosition = PrimaryPointer.Result.EndPoint;
            var pointerGrabPoint = PrimaryPointer.Result.GrabPoint;
            var pointerDirection = PrimaryPointer.Result.Direction;

            if (pointerDirection.Equals(Vector3.zero)) { return; }

            if (!IsTranslateLocked)
            {
                var currentPosition = manipulationTarget.position;
                var targetPosition = pointerGrabPoint == Vector3.zero
                    ? pointerPosition
                    : (pointerPosition + currentPosition) - pointerGrabPoint;
                var sweepFailed = !body.SweepTest(pointerDirection, out var sweepHitInfo);
                var targetDirection = targetPosition - currentPosition;
                var targetDistance = targetDirection.magnitude;
                var lastHitObject = PrimaryPointer.Result.LastHitObject;

                var scale = manipulationTarget.localScale;
                var bounds = Collider.bounds;
                var scaledSize = bounds.size * scale.y;
                var scaledCenter = bounds.center * scale.y;
                var isValidMove = !sweepFailed && sweepHitInfo.distance > targetDistance;
                var hitDown = TryGetRaycastBoundsCorners(snapDistance, Vector3.down, out _, out _, out var maxHitDown);

                float CalculateVerticalPosition(RaycastHit hit)
                {
                    var hitPoint = manipulationTarget.TransformPoint(hit.point);
                    return hitPoint.y + (scaledSize.y * 0.5f - scaledCenter.y) + 0.01f;
                }

                if (IsSnappedToSurface)
                {
                    var lastHit = lastHitObject == null
                        ? sweepHitInfo.transform
                        : lastHitObject.transform;
                    var hitNew = sweepFailed && (lastHit != snapTarget || lastHit == null) && !hitDown;

                    if (targetDistance > unsnapTolerance || hitNew)
                    {
                        IsSnappedToSurface = false;
                    }

                    // Check any overrides
                    if (!IsSnappedToSurface)
                    {
                        snapTarget = null;
                        OnUnsnap();
                    }
                    else if (hitDown)
                    {
                        // If we're still snapped to the surface then place the vertical
                        // position at the highest hit point to "follow" the surface.
                        snappedVerticalPosition = CalculateVerticalPosition(maxHitDown);
                    }
                }

                var justSnapped = false;
                var isValidSnap = !sweepFailed && sweepHitInfo.distance <= snapDistance;

                if (!sweepFailed)
                {
                    if (!isValidMove)
                    {
                        if (IsSnappedToSurface)
                        {
                            targetPosition = currentPosition;
                        }

                        isValidSnap = false;
                    }

                    var color = isValidMove ? isValidSnap ? Color.green : Color.yellow : Color.red;
                    DebugUtilities.DrawPoint(sweepHitInfo.point, color);
                    Debug.DrawLine(sweepHitInfo.point, currentPosition, color);

                    if (snapToValidSurfaces &&
                        !IsSnappedToSurface &&
                        isValidSnap &&
                        sweepHitInfo.normal.IsValidVector() &&
                        sweepHitInfo.normal.IsNormalVertical())
                    {
                        snapTarget = lastHitObject != null
                            ? lastHitObject.transform == manipulationTarget
                                ? sweepHitInfo.transform
                                : lastHitObject.transform
                            : sweepHitInfo.transform;

                        Debug.Assert(snapTarget != null);
                        snappedVerticalPosition = CalculateVerticalPosition(sweepHitInfo);
                        IsSnappedToSurface = true;
                        justSnapped = true;
                        OnSnap();
                    }
                }

                if (IsSnappedToSurface)
                {
                    targetPosition.y = snappedVerticalPosition;
                }

                if (!justSnapped && smoothMotion && !IsRotating && !IsNudgePossible && !IsScalingPossible)
                {
                    targetPosition = Vector3.Lerp(manipulationTarget.position, targetPosition, Time.deltaTime * smoothingFactor);
                }

                manipulationTarget.position = targetPosition;
            }

            var pivot = pointerGrabPoint == Vector3.zero ? pointerPosition : pointerGrabPoint;

            if (IsRotating)
            {
                manipulationTarget.RotateAround(pivot, Vector3.up, -updatedAngle);
                updatedAngle = 0f;
            }
            else if (IsNudgePossible)
            {
                PrimaryPointer.PointerExtent = updatedExtent;
            }
            else if (IsScalingPossible)
            {
                manipulationTarget.ScaleAround(pivot, updatedScale);
            }
        }

        /// <summary>
        /// Raycast from the bounds corners in the specified direction.
        /// </summary>
        /// <param name="distance">The distance to perform the raycast.</param>
        /// <param name="direction">The direction to perform the raycast.</param>
        /// <param name="hitAllCorners">Did the raycast hit for all of the valid corners in the direction of the raycast?</param>
        /// <param name="minHitDistance">The closest hit.</param>
        /// <param name="maxHitDistance">The furthest hit.</param>
        /// <returns>True, if any of the raycasts hit.</returns>
        protected bool TryGetRaycastBoundsCorners(float distance, Vector3 direction, out bool hitAllCorners, out RaycastHit minHitDistance, out RaycastHit maxHitDistance)
        {
            minHitDistance = default;
            maxHitDistance = default;
            hitAllCorners = true;

            Vector3[] boundsCorners = null;
            Collider.GetCornerPositionsWorldSpace(manipulationTarget, ref boundsCorners);

            var hitAny = false;
            var scaledCenter = Collider.bounds.center;
            DebugUtilities.DrawPoint(scaledCenter, Color.cyan, 0.1f);

            for (int i = 0; i < boundsCorners.Length; i++)
            {
                var cornerPosition = boundsCorners[i];
                var directionFromCenter = scaledCenter - cornerPosition;

                // Raycast out in all directions.
                if (direction == Vector3.zero)
                {
                    direction = directionFromCenter;
                }

                var dot = Vector3.Dot(manipulationTarget.TransformDirection(direction), directionFromCenter);

                if (dot >= 0f) { continue; }

                var rayStep = new RayStep(new Ray(cornerPosition, direction), distance);

                if (MixedRealityRaycaster.RaycastSimplePhysicsStep(rayStep, LayerMasks, out var hitInfo))
                {
                    hitAny = true;

                    if (hitInfo.distance < minHitDistance.distance)
                    {
                        minHitDistance = hitInfo;
                    }

                    if (hitInfo.distance > maxHitDistance.distance)
                    {
                        maxHitDistance = hitInfo;
                    }

                    if (hitInfo.distance > distance)
                    {
                        hitAllCorners = false;
                    }

                    DebugUtilities.DrawPoint(hitInfo.point, Color.yellow);
                    Debug.DrawLine(hitInfo.point, cornerPosition, Color.yellow);
                }
                else
                {
                    hitAllCorners = false;
                    Debug.DrawLine(scaledCenter, cornerPosition, Color.red);
                }
            }

            return hitAny;
        }
    }
}
