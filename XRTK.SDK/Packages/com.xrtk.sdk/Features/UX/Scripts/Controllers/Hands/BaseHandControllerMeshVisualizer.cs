// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public abstract class BaseHandControllerMeshVisualizer : MonoBehaviour, IMixedRealityHandMeshHandler
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

        public abstract void OnMeshUpdated(InputEventData<HandMeshData> eventData);
    }
}