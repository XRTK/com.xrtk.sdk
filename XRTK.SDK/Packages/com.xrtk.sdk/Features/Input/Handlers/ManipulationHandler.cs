// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.InputSystem;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
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
        [Tooltip("The dual axis zone to process and activate the scale action.\nNote: this is transformed into and used as absolute values.")]
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
        private Vector2 scaleConstraints = new Vector2(0.01f, 1f);

        /// <summary>
        /// The min and max size this object can be scaled to.
        /// </summary>
        public Vector2 ScaleConstraints
        {
            get => scaleConstraints;
            set => scaleConstraints = value;
        }

        [SerializeField]
        [Tooltip("The dual axis zone to process and activate the rotation action.\nNote: this is transformed into and used as absolute values.")]
        private Vector2 rotationZone = new Vector2(1f, 0.25f);

        /// <summary>
        /// The dual axis zone to process and activate the rotation action.
        /// </summary>
        /// <remarks>This is transformed into and used as absolute values.</remarks>
        public Vector2 RotationZone
        {
            get => rotationZone;
            set => rotationZone = value;
        }

        [SerializeField]
        [Tooltip("The amount to rotate the GameObject")]
        private float rotationAmount = 1f;

        /// <summary>
        /// The amount to rotate the <see cref="GameObject"/>
        /// </summary>
        public float RotationAmount
        {
            get => rotationAmount;
            set => rotationAmount = value;
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

        /// <summary>
        /// The first pointer to start the manipulation phase of this object.
        /// </summary>
        private IMixedRealityPointer primaryPointer = null;

        #region Monobehaviour Implementation

        private void Awake()
        {
            if (manipulationTarget == null)
            {
                manipulationTarget = transform;
            }
        }

        private void Update()
        {
            if (isBeingHeld)
            {
                transform.position = primaryPointer.Result.Details.Point;
            }
        }

        #endregion Monobehaviour Implementation

        #region IMixedRealityInputHandler Implementation

        /// <inheritdoc />
        public virtual void OnInputDown(InputEventData eventData)
        {
        }

        /// <inheritdoc />
        public virtual void OnInputUp(InputEventData eventData)
        {
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData)
        {
            if (!isBeingHeld ||
                primaryInputSource == null ||
                eventData.InputSource.SourceId != primaryInputSource.SourceId)
            {
                return;
            }

            var absoluteInputData = eventData.InputData;
            absoluteInputData.x = Mathf.Abs(absoluteInputData.x);
            absoluteInputData.y = Mathf.Abs(absoluteInputData.y);

            bool isScalePossible = eventData.MixedRealityInputAction == scaleAction && absoluteInputData.y > 0f;
            bool isRotatePossible = eventData.MixedRealityInputAction == rotateAction && absoluteInputData.x > 0f;

            if (isScalePossible &&
                absoluteInputData.x >= scaleZone.x)
            {
                isScalePossible = false;
            }

            if (isRotatePossible &&
                absoluteInputData.y >= RotationZone.y)
            {
                isRotatePossible = false;
            }

            if (absoluteInputData.y <= RotationZone.y && absoluteInputData.x <= scaleZone.x)
            {
                isScalePossible = false;
                isRotatePossible = false;
            }

            if (isRotatePossible && isScalePossible)
            {
                isScalePossible = false;
                isRotatePossible = false;
            }

            if (isScalePossible)
            {
                var newScale = transform.localScale;
                var prevScale = newScale;

                if (eventData.InputData.y < 0f)
                {
                    newScale *= scaleAmount;

                    // We can check any axis, they should all be the same.
                    if (newScale.x <= scaleConstraints.x)
                    {
                        newScale = prevScale;
                    }
                }
                else
                {
                    newScale /= scaleAmount;

                    // We can check any axis, they should all be the same.
                    if (newScale.y >= scaleConstraints.y)
                    {
                        newScale = prevScale;
                    }
                }

                transform.localScale = newScale;
                eventData.Use();
            }

            if (isRotatePossible)
            {
                if (eventData.InputData.x < 0f)
                {
                    transform.Rotate(0f, rotationAmount, 0f, Space.Self);
                }
                else
                {
                    transform.Rotate(0f, -rotationAmount, 0f, Space.Self);
                }

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
        public virtual void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        #endregion IMixedRealityPointerHandler Implementation

        private void BeginHold(MixedRealityPointerEventData eventData)
        {
            isBeingHeld = true;
            MixedRealityToolkit.InputSystem.PushModalInputHandler(gameObject);

            if (primaryInputSource == null)
            {
                primaryInputSource = eventData.InputSource;
            }

            if (primaryPointer == null)
            {
                primaryPointer = eventData.Pointer;
            }

            transform.SetCollidersActive(false);
        }

        private void EndHold(MixedRealityPointerEventData eventData)
        {
            if (primaryInputSource != null &&
                primaryInputSource.SourceId == eventData.InputSource.SourceId)
            {
                primaryPointer = null;
                primaryInputSource = null;
            }

            isBeingHeld = false;
            MixedRealityToolkit.InputSystem.PopModalInputHandler();
            transform.SetCollidersActive(true);
        }
    }
}
