// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.SDK.UX;
using XRTK.Services;
using XRTK.Utilities;

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

        public float SmoothingFactor
        {
            get => smoothingFactor;
            set => smoothingFactor = value;
        }

        [SerializeField]
        private bool snapToValidSurfaces = true;

        public bool SnapToValidSurfaces
        {
            get => snapToValidSurfaces;
            set => snapToValidSurfaces = value;
        }

        [SerializeField]
        [Tooltip("This distance that will make the object snap to the surface.")]
        private float snapDistance = 0.0762f;

        public float SnapDistance
        {
            get => snapDistance;
            set => snapDistance = value;
        }

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

        /// <summary>
        /// Is the <see cref="GameObject"/> currently snapped to a surface?
        /// </summary>
        public bool IsSnappedToSurface { get; private set; } = false;

        private BoxCollider boxCollider;

        public BoxCollider BoxCollider
        {
            get
            {
                if (BoundingBox != null)
                {
                    return BoundingBox.BoundingBoxCollider;
                }

                if (boxCollider == null)
                {
                    boxCollider = gameObject.EnsureComponent<BoxCollider>();
                    transform.GetColliderBounds();
                }

                return boxCollider;
            }
        }

        public BoundingBox BoundingBox { get; private set; }

        /// <summary>
        /// The captured primary pointer for the current active hold.
        /// </summary>
        public IMixedRealityPointer PrimaryPointer { get; private set; }

        #endregion Properties

        private IMixedRealityInputSource primaryInputSource;

        private int prevPhysicsLayer;
        private SpatialMeshDisplayOptions prevSpatialMeshDisplay;

        private float updatedAngle;
        private float updatedExtent;
        private float prevPointerExtent;

        private Vector2 lastPositionReading;

        private Vector3 prevScale;
        private Vector3 prevPosition;
        private Vector3 updatedScale;
        private Vector3 offsetPosition;

        private Quaternion prevRotation;

        private Rigidbody body;

        private float snappedVerticalPosition;

        private Transform snapTarget;

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

        private void Update()
        {
            for (var keyCode = KeyCode.Alpha0; keyCode < KeyCode.Tilde; keyCode++)
            {
                if (UnityEngine.Input.GetKey(keyCode))
                {
                    const float nudgeFactor = 0.5f;
                    float prevExtent;
                    float newExtent;

                    const float scaleFactor = 0.1f;
                    Vector3 newScale;

                    switch (keyCode)
                    {
                        case KeyCode.W:
                            IsNudgePossible = true;
                            prevExtent = GetCurrentExtent(PrimaryPointer);
                            newExtent = CalculateNudgeDistance(nudgeFactor, prevExtent);
                            updatedExtent = ClampExtent(nudgeFactor, newExtent, prevExtent);
                            break;
                        case KeyCode.A:
                            IsScalingPossible = true;
                            newScale = CalculateScaleAmount(scaleFactor, manipulationTarget.localScale);
                            updatedScale = ClampScale(scaleFactor, newScale);
                            break;
                        case KeyCode.S:
                            IsNudgePossible = true;
                            prevExtent = GetCurrentExtent(PrimaryPointer);
                            newExtent = CalculateNudgeDistance(-nudgeFactor, prevExtent);
                            updatedExtent = ClampExtent(-nudgeFactor, newExtent, prevExtent);
                            break;
                        case KeyCode.D:
                            IsScalingPossible = true;
                            newScale = CalculateScaleAmount(-scaleFactor, manipulationTarget.localScale);
                            updatedScale = ClampScale(-scaleFactor, newScale);
                            break;
                        case KeyCode.Q:
                            updatedAngle += 2.8125f;
                            break;
                        case KeyCode.E:
                            updatedAngle -= 2.8125f;
                            break;
                    }
                }

                if (UnityEngine.Input.GetKeyUp(keyCode))
                {
                    switch (keyCode)
                    {
                        case KeyCode.W:
                            IsNudgePossible = false;
                            break;
                        case KeyCode.A:
                            IsScalingPossible = false;
                            break;
                        case KeyCode.S:
                            IsNudgePossible = false;
                            break;
                        case KeyCode.D:
                            IsScalingPossible = false;
                            break;
                    }
                }
            }
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
                // We don't pass IsCancelled here because 
                // it's the intended behaviour to end the hold
                // if the component is disabled.
                EndHold();
            }
        }

        protected virtual void OnCollisionEnter(Collision other)
        {
            //Debug.LogWarning($"OnCollisionEnter {name} -> {other.gameObject.name}");
        }

        protected virtual void OnCollisionStay(Collision other)
        {
            //Debug.LogWarning($"OnCollisionStay {name} -> {other.gameObject.name}");
        }

        protected virtual void OnCollisionExit(Collision other)
        {
            //Debug.LogWarning($"OnCollisionExit {name} -> {other.gameObject.name}");
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

            if (PrimaryPointer == null)
            {
                PrimaryPointer = eventData.Pointer;
            }

            MixedRealityToolkit.InputSystem.PushModalInputHandler(gameObject);

            if (MixedRealityToolkit.SpatialAwarenessSystem != null)
            {
                prevSpatialMeshDisplay = MixedRealityToolkit.SpatialAwarenessSystem.SpatialMeshVisibility;
                MixedRealityToolkit.SpatialAwarenessSystem.SpatialMeshVisibility = spatialMeshVisibility;
            }

            prevPosition = manipulationTarget.position;

            if (prevPosition == Vector3.zero)
            {
                manipulationTarget.position = PrimaryPointer.Result.EndPoint;
            }
            else
            {
                offsetPosition = prevPosition - PrimaryPointer.Result.EndPoint;

                // update the pointer extent to prevent the object from popping to the end of the pointer
                prevPointerExtent = PrimaryPointer.PointerExtent;
                var currentRaycastDistance = PrimaryPointer.Result.RayDistance;
                PrimaryPointer.PointerExtent = currentRaycastDistance;
            }

            prevScale = manipulationTarget.localScale;
            prevRotation = manipulationTarget.rotation;

            PrimaryPointer.SyncedTarget = gameObject;

            if (BoundingBox == null)
            {
                prevPhysicsLayer = gameObject.layer;
                transform.SetLayerRecursively(IgnoreRaycastLayer);
            }

            transform.SetCollidersActive(false);
            BoxCollider.enabled = true;

            body.isKinematic = false;

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

            MixedRealityToolkit.InputSystem.PopModalInputHandler();

            if (BoundingBox == null)
            {
                transform.SetLayerRecursively(prevPhysicsLayer);
            }

            transform.SetCollidersActive(true);
            body.isKinematic = true;

            PrimaryPointer.IsFocusLocked = false;
            PrimaryPointer = null;
            primaryInputSource = null;

            offsetPosition = Vector3.zero;

            IsBeingHeld = false;
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
        protected virtual void OnSnap()
        {
        }

        /// <summary>
        /// Do stuff when snap stops.
        /// </summary>
        protected virtual void OnUnsnap()
        {
        }

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
            if (!IsBeingHeld || PrimaryPointer == null) { return; }

            var pointerPosition = PrimaryPointer.Result.EndPoint;
            var pointerOffsetPosition = PrimaryPointer.Result.Offset;
            var pointerDirection = PrimaryPointer.Result.Direction;

            if (pointerDirection.Equals(Vector3.zero)) { return; }
            if (pointerOffsetPosition == Vector3.zero) { return; }

            var currentPosition = manipulationTarget.position;
            var targetPosition = offsetPosition + pointerPosition;

            var sweepPass = !body.SweepTest(pointerDirection, out var hitInfo);
            var objectHeight = manipulationTarget.TransformPoint(BoxCollider.size * 0.5f);
            var lastHitObject = PrimaryPointer.Result.LastHitObject;

            if (IsSnappedToSurface)
            {
                var lastHit = lastHitObject == null
                    ? hitInfo.transform
                    : lastHitObject.transform;

                var hitNew = sweepPass && lastHit != snapTarget && pointerPosition.y < currentPosition.y;

                if (hitNew &&
                    hitInfo.transform == null &&
                    Physics.Raycast(PrimaryPointer.Rays[PrimaryPointer.Result.RayStepIndex], out var sweepHit))
                {
                    BoxCollider.bounds.Contains(sweepHit.point);
                    hitNew = false;
                }

                var pointerMovedAway = pointerDirection.y > 0f && pointerPosition.y > objectHeight.y;

                if (pointerMovedAway || hitNew)
                {
                    snapTarget = null;
                    IsSnappedToSurface = false;

                    OnUnsnap();
                }
            }

            var justSnapped = false;

            if (!sweepPass &&
                snapToValidSurfaces &&
                !IsSnappedToSurface &&
                hitInfo.normal.IsValidVector() &&
                hitInfo.normal.IsNormalVertical() &&
                hitInfo.distance <= snapDistance)
            {
                snapTarget = lastHitObject != null
                    ? lastHitObject.transform == manipulationTarget
                        ? hitInfo.transform
                        : lastHitObject.transform
                    : hitInfo.transform;

                var scale = manipulationTarget.localScale;
                var scaledSize = BoxCollider.size * scale.y;
                var scaledCenter = BoxCollider.center * scale.y;

                snappedVerticalPosition = hitInfo.point.y + (scaledSize.y * 0.5f - scaledCenter.y);
                IsSnappedToSurface = true;
                justSnapped = true;
                OnSnap();
            }

            if (IsSnappedToSurface)
            {
                targetPosition.y = snappedVerticalPosition;
            }

            DebugUtilities.DrawPoint(targetPosition, Color.blue);
            Debug.DrawLine(targetPosition, manipulationTarget.position, Color.blue);

            if (!justSnapped && smoothMotion && !IsRotating && !IsNudgePossible && !IsScalingPossible)
            {
                targetPosition = Vector3.Lerp(manipulationTarget.position, targetPosition, Time.deltaTime * smoothingFactor);
            }

            manipulationTarget.position = targetPosition;

            if (IsRotating)
            {
                manipulationTarget.RotateAround(pointerPosition, Vector3.up, -updatedAngle);
                updatedAngle = 0f;

                if (prevPosition != Vector3.zero)
                {
                    offsetPosition = manipulationTarget.position - pointerPosition;
                }
            }
            else if (IsNudgePossible)
            {
                PrimaryPointer.PointerExtent = updatedExtent;
            }
            else if (IsScalingPossible)
            {
                manipulationTarget.ScaleAround(pointerPosition, updatedScale);

                if (prevPosition != Vector3.zero)
                {
                    offsetPosition = manipulationTarget.position - pointerPosition;
                }
            }
        }
    }
}
