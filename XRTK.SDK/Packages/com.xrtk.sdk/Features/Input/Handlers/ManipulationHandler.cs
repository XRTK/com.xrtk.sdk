// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using XRTK.Definitions;
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
        public MixedRealityInputAction SelectAction => selectAction;

        [SerializeField]
        [Tooltip("The action to use to nudge the GameObject closer or further away from the raycast pointer source")]
        private MixedRealityInputAction nudgeAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to nudge the GameObject closer or further away from the raycast pointer source
        /// </summary>
        public MixedRealityInputAction NudgeAction => nudgeAction;

        [SerializeField]
        [Tooltip("The action to use to rotate the GameObject")]
        private MixedRealityInputAction rotateAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to rotate the GameObject
        /// </summary>
        public MixedRealityInputAction RotateAction => rotateAction;

        [SerializeField]
        [Tooltip("The action to use to scale the GameObject")]
        private MixedRealityInputAction scaleAction = MixedRealityInputAction.None;

        /// <summary>
        /// The action to use to scale the GameObject
        /// </summary>
        public MixedRealityInputAction ScaleAction => scaleAction;

        #endregion Input Actions

        #region Manipulation Options

        [Header("Options")]

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

        #endregion Manipulation Options

        /// <summary>
        /// The current status of the hold.
        /// </summary>
        /// <remarks>
        /// Used to determine if the <see cref="GameObject"/> is currently being manipulated by the user.
        /// </remarks>
        [NonSerialized]
        private bool isBeingHeld;

        /// <summary>
        /// The first input source to start the manipulation phase of this object.
        /// </summary>
        [NonSerialized]
        private IMixedRealityInputSource primaryInputSource = null;

        /// <summary>
        /// The second input source to start the manipulation phase of this object.
        /// </summary>
        /// <remarks>
        /// Used for two handed manipulations
        /// </remarks>
        [NonSerialized]
        private IMixedRealityInputSource secondaryInputSource = null;

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
                else if (secondaryInputSource == null)
                {
                    secondaryInputSource = eventData.InputSource;
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnInputUp(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                var resetSecondary = false;

                if (primaryInputSource != null &&
                    primaryInputSource.SourceId == eventData.InputSource.SourceId)
                {
                    // Clear both primary and secondary input sources.
                    ResetPointerExtents(primaryInputSource);
                    primaryInputSource = null;
                    resetSecondary = true;
                }

                if (resetSecondary ||
                    secondaryInputSource != null &&
                    eventData.InputSource.SourceId == secondaryInputSource.SourceId)
                {
                    ResetPointerExtents(secondaryInputSource);
                    secondaryInputSource = null;
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnInputChanged(InputEventData<Vector2> eventData)
        {
            if (eventData.MixedRealityInputAction == nudgeAction)
            {
                if (eventData.InputSource.SourceId == primaryInputSource.SourceId)
                {
                    var closer = eventData.InputData.y < 0;
                    var pointers = eventData.InputSource.Pointers;

                    for (int i = 0; i < pointers.Length; i++)
                    {
                        pointers[i].PointerExtent = closer ? -nudgeAmount : nudgeAmount;
                    }
                }
            }

            if (eventData.MixedRealityInputAction == rotateAction)
            {
                if (eventData.InputSource.SourceId == primaryInputSource.SourceId)
                {
                }
            }

            if (eventData.MixedRealityInputAction == scaleAction)
            {
                if (eventData.InputSource.SourceId == primaryInputSource.SourceId)
                {
                }
            }
        }

        #endregion IMixedRealityInputHandler Implementation

        private void ResetPointerExtents(IMixedRealityInputSource source)
        {
            var pointers = source.Pointers;

            for (int i = 0; i < pointers.Length; i++)
            {
                pointers[i].PointerExtent = pointers[i].DefaultPointerExtent;
            }
        }
    }
}
