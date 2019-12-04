// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public abstract class BaseHandControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer, IMixedRealityHandDataHandler
    {
        private IMixedRealityHandControllerDataProvider dataProvider;

        [SerializeField]
        [Tooltip("Should a gizmo be drawn to represent the hand bounds.")]
        private bool drawBoundsGizmos = true;

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
        /// Should a gizmo be drawn to represent the hand bounds.
        /// </summary>
        public bool DrawBoundsGizmos
        {
            get { return drawBoundsGizmos; }
            set { drawBoundsGizmos = value; }
        }

        /// <summary>
        /// The currently active hand visualization profile.
        /// </summary>
        protected MixedRealityHandControllerVisualizationProfile Profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        /// <summary>
        /// Executes when the visualizer is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            dataProvider = MixedRealityToolkit.GetService<IMixedRealityHandControllerDataProvider>();
            dataProvider.Register(this);
        }

        /// <summary>
        /// Called by the Unity runtime when gizmos should be drawn.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (drawBoundsGizmos)
            {
                foreach (int trackedHandBounds in Enum.GetValues(typeof(TrackedHandBounds)))
                {
                    foreach (var controller in dataProvider.ActiveControllers)
                    {
                        if (controller.ControllerHandedness == Handedness
                            && controller is IMixedRealityHandController handController
                            && handController.TryGetBounds((TrackedHandBounds)trackedHandBounds, out Bounds? bounds))
                        {
                            Gizmos.DrawWireCube(bounds.Value.center, bounds.Value.size);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            dataProvider.Unregister(this);
            base.OnDisable();
        }

        /// <inheritdoc />
        public virtual void OnHandDataUpdated(InputEventData<HandData> eventData)
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