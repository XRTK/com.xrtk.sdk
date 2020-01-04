// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public abstract class BaseHandControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer
    {
        //private IMixedRealityHandControllerDataProvider dataProvider = null;

        [SerializeField]
        [Tooltip("Renders the hand joints. Note: this could reduce performance.")]
        private bool enableHandJointVisualization = true;

        [SerializeField]
        [Tooltip("Renders the hand mesh, if available. Note: this could reduce performance.")]
        private bool enableHandMeshVisualization = false;

        /// <summary>
        /// Is hand joint rendering enabled?
        /// </summary>
        protected bool EnableHandJointVisualization => enableHandJointVisualization;

        /// <summary>
        /// Is hand mesh rendering enabled?
        /// </summary>
        protected bool EnableHandMeshVisualization => enableHandMeshVisualization;

        /// <inheritdoc />
        public GameObject GameObjectProxy
        {
            get
            {
                try
                {
                    return gameObject;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Called by the Unity runtime when gizmos should be drawn.
        /// </summary>
        //private void OnDrawGizmos()
        //{
        //    foreach (int trackedHandBounds in Enum.GetValues(typeof(TrackedHandBounds)))
        //    {
        //        foreach (var controller in dataProvider.ActiveControllers)
        //        {
        //            if (controller.ControllerHandedness == Handedness
        //                && controller is IMixedRealityHandController handController
        //                && handController.TryGetBounds((TrackedHandBounds)trackedHandBounds, out Bounds? bounds))
        //            {
        //                Gizmos.DrawWireCube(bounds.Value.center, bounds.Value.size);
        //            }
        //        }
        //    }
        //}

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            if (eventData.Handedness != Controller.ControllerHandedness)
            {
                return;
            }

            HandData handData = eventData.InputData;
            UpdateHandJointVisualization(handData);
            UpdateHandJointVisualization(handData);
        }

        /// <summary>
        /// Updates hand joint visulization with latest available hand data.
        /// </summary>
        /// <param name="handData">Updated hand data.</param>
        protected abstract void UpdateHandJointVisualization(HandData handData);

        /// <summary>
        /// Updates hand mesh visulization with latest available hand data.
        /// </summary>
        /// <param name="handData">Updated hand data.</param>
        protected abstract void UpdateHansMeshVisualization(HandData handData);
    }
}