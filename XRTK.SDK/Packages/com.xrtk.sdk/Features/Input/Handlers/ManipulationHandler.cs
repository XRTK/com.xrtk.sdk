// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// This input handler is designed to help facilitate the physical manipulation of <see cref="GameObject"/>s across all platforms.
    /// Users will be able to use the select action to activate the manipulation phase, then various gestures and supplemental actions to
    /// nudge, rotate, and scale the object.
    /// </summary>
    public class ManipulationHandler : BaseInputHandler,
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

        #region Monobehaviour Implementation

        private void Awake()
        {
            if (manipulationTarget == null)
            {
                manipulationTarget = transform;
            }
        }

        #endregion Monobehaviour Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData)
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
        public virtual void OnInputUp(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                if (useHold)
                {
                    if (isBeingHeld)
                    {
                        EndHold(eventData);
                    }
                    else
                    {
                        BeginHold(eventData);
                    }
                }

                if (!useHold && isBeingHeld)
                {
                    EndHold(eventData);
                }

                eventData.Use();
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData)
        {
            if (!isBeingHeld) { return; }

            if (primaryInputSource != null &&
                eventData.MixedRealityInputAction == scaleAction)
            {
                if (eventData.InputSource.SourceId == primaryInputSource.SourceId)
                {
                    if (Mathf.Abs(eventData.InputData.y) >= 0.1f) { return; }

                    if (eventData.InputData.x < 0)
                    {
                        transform.localScale *= scaleAmount;
                    }
                    else
                    {
                        transform.localScale /= scaleAmount;
                    }
                }

                eventData.Use();
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        private void BeginHold(InputEventData eventData)
        {
            isBeingHeld = true;

            if (primaryInputSource == null)
            {
                primaryInputSource = eventData.InputSource;
            }
        }

        private void EndHold(InputEventData eventData)
        {
            if (primaryInputSource != null &&
                primaryInputSource.SourceId == eventData.InputSource.SourceId)
            {
                primaryInputSource = null;
            }

            isBeingHeld = false;
        }
    }
}
