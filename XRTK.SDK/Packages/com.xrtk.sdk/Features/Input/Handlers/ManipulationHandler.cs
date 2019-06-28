// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
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

        #endregion Input Actions

        #region Manipulation Options

        [Header("Options")]

        [SerializeField]
        [Tooltip("The object to manipulate using this handler. Automatically uses this transform if none is set.")]
        private Transform manipulationTarget;

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
        [Range(0.1f, 0.001f)]
        [Tooltip("The amount to scale the GameObject")]
        private float scaleAmount = 0.01f;

        /// <summary>
        /// The amount to scale the <see cref="GameObject"/>
        /// </summary>
        public float ScaleAmount
        {
            get => scaleAmount;
            set => scaleAmount = value;
        }

        //[PhysicsLayer]
        //[SerializeField]
        //[Tooltip("The physics layer to place the object in while doing manipulations")]
        //private int manipulationLayer = 0;

        ///// <summary>
        ///// The default physics layer this <see cref="GameObject"/> is usually on when not in the manipulation phase.
        ///// </summary>
        //private int defaultLayer = 0;

        #endregion Manipulation Options

        /// <summary>
        /// The current status of the hold.
        /// </summary>
        /// <remarks>
        /// Used to determine if the <see cref="GameObject"/> is currently being manipulated by the user.
        /// </remarks>
        private bool isBeingHeld = false;

        /// <summary>
        /// The first input source to start the manipulation phase of this object.
        /// </summary>
        private IMixedRealityInputSource primaryInputSource = null;

        /// <summary>
        /// The first pointer to start the manipulation phase of this object.
        /// </summary>
        private IMixedRealityPointer primaryPointer = null;

        ///// <summary>
        ///// The second input source detected during the manipulation phase of this object.
        ///// </summary>
        ///// <remarks>
        ///// Used for two handed manipulations
        ///// </remarks>
        //private IMixedRealityInputSource secondaryInputSource = null;

        ///// <summary>
        ///// The second pointer detected during the manipulation phase of this object.
        ///// </summary>
        //private IMixedRealityPointer secondaryPointer = null;

        ///// <summary>
        ///// The collider used when doing manipulations.
        ///// </summary>
        //private Collider manipulationCollider = null;

        #region Monobehaviour Implementation

        private void Awake()
        {
            if (manipulationTarget == null)
            {
                manipulationTarget = transform;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        private void Update()
        {
            if (isBeingHeld)
            {
                transform.position = primaryPointer != null
                    ? primaryPointer.Result.Details.Point
                    : MixedRealityToolkit.InputSystem.GazeProvider.GazePointer.Result.Details.Point;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        #endregion Monobehaviour Implementation

        #region IMixedRealityPointerHandler Implementation

        /// <inheritdoc />
        public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                if (primaryInputSource == null)
                {
                    primaryPointer = eventData.Pointer;
                    primaryInputSource = eventData.InputSource;
                }
                //else if (secondaryInputSource == null)
                //{
                //    secondaryPointer = eventData.Pointer;
                //    secondaryInputSource = eventData.InputSource;
                //}
            }
        }

        /// <inheritdoc />
        public virtual void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                // var resetSecondary = false;

                if (primaryInputSource != null &&
                    primaryInputSource.SourceId == eventData.InputSource.SourceId)
                {
                    // Clear both primary and secondary input sources.
                    primaryPointer = null;
                    primaryInputSource = null;
                    // resetSecondary = true;
                }

                //if (secondaryInputSource != null &&
                //    (resetSecondary ||
                //     eventData.InputSource.SourceId == secondaryInputSource.SourceId))
                //{
                //    secondaryPointer = null;
                //    secondaryInputSource = null;
                //}
            }
        }

        /// <inheritdoc />
        public virtual void OnPointerClicked(MixedRealityPointerEventData eventData) { }

        #endregion IMixedRealityPointerHandler Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                if (primaryInputSource == null)
                {
                    primaryInputSource = eventData.InputSource;
                }
                //else if (secondaryInputSource == null)
                //{
                //    secondaryInputSource = eventData.InputSource;
                //}
            }
        }

        /// <inheritdoc />
        public virtual void OnInputUp(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                // var resetSecondary = false;

                if (primaryInputSource != null &&
                    primaryInputSource.SourceId == eventData.InputSource.SourceId)
                {
                    // Clear both primary and secondary input sources.
                    primaryPointer = null;
                    primaryInputSource = null;
                    // resetSecondary = true;
                }

                //if (secondaryInputSource != null &&
                //    (resetSecondary ||
                //     eventData.InputSource.SourceId == secondaryInputSource.SourceId))
                //{
                //    secondaryPointer = null;
                //    secondaryInputSource = null;
                //}
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData)
        {
            if (!isBeingHeld) { return; }

            if (primaryInputSource != null &&
                eventData.MixedRealityInputAction == rotateAction)
            {
                if (eventData.InputSource.SourceId == primaryInputSource.SourceId)
                {
                }
            }

            if (primaryInputSource != null &&
                eventData.MixedRealityInputAction == scaleAction)
            {
                if (eventData.InputSource.SourceId == primaryInputSource.SourceId)
                {
                    if (Mathf.Abs(eventData.InputData.y) >= 0.1f) { return; }

                    var smaller = eventData.InputData.x < 0;
                    transform.localScale *= smaller ? -scaleAmount : scaleAmount;
                }
            }
        }

        #endregion IMixedRealityInputHandler Implementation
    }
}
