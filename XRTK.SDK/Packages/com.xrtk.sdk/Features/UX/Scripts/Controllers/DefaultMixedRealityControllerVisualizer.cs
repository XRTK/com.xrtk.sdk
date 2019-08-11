// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Interfaces.Providers.Controllers;
using System;
using UnityEngine;
using XRTK.SDK.Input.Handlers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.UX.Controllers.Hands;
using XRTK.Services;
using XRTK.Definitions.Controllers;

namespace XRTK.SDK.UX.Controllers
{
    /// <summary>
    /// The Mixed Reality Visualization component is primarily responsible for synchronizing the user's current input with controller models.
    /// </summary>
    /// <seealso cref="Providers.Controllers.MixedRealityControllerVisualizationProfile"/>
    public class DefaultMixedRealityControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer
    {
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

        protected override void Start()
        {
            base.Start();

            if (typeof(BaseHandController).IsAssignableFrom(Controller.GetType()))
            {
                MixedRealityHandControllerVisualizationProfile handControllerVisualizationProfile = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

                if (handControllerVisualizationProfile.EnableHandJointVisualization)
                {
                    GameObjectProxy.AddComponent<BaseHandControllerJointVisualizer>();
                }

                if (handControllerVisualizationProfile.EnableHandMeshVisualization)
                {
                    GameObjectProxy.AddComponent<BaseHandControllerMeshVisualizer>();
                }
            }
        }
    }
}