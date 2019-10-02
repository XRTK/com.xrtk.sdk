// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public abstract class BaseHandControllerJointVisualizer : MonoBehaviour, IMixedRealityHandJointHandler
    {
        private IMixedRealityHandControllerDataProvider dataProvider;

        /// <summary>
        /// The currently active hand visualization profile.
        /// </summary>
        protected MixedRealityHandControllerVisualizationProfile Profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        protected virtual void Start()
        {
            dataProvider = MixedRealityToolkit.GetService<IMixedRealityHandControllerDataProvider>();
            dataProvider.Register(this);
        }

        protected virtual void OnDestroy()
        {
            dataProvider.Unregister(this);
        }

        public abstract void OnJointUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData);
    }
}