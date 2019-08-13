// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.InputSystem.Simulation;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Default hand controller visualizer for hand joints.
    /// </summary>
    [RequireComponent(typeof(DefaultMixedRealityControllerVisualizer))]
    public class DefaultHandControllerJointVisualizer : MonoBehaviour, IMixedRealityHandJointHandler
    {
        private DefaultMixedRealityControllerVisualizer controllerVisualizer;
        private IHandTrackingSimulationDataProvider dataProvider;
        private Dictionary<TrackedHandJoint, Transform> joints;

        /// <summary>
        /// The currently active hand visualization profile.
        /// </summary>
        private MixedRealityHandControllerVisualizationProfile profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        /// <summary>
        /// Provides access to the joint transforms used for visualization.
        /// </summary>
        public IReadOnlyDictionary<TrackedHandJoint, Transform> Joints => joints;

        private void Start()
        {
            controllerVisualizer = GetComponent<DefaultMixedRealityControllerVisualizer>();
            joints = new Dictionary<TrackedHandJoint, Transform>();
            dataProvider = MixedRealityToolkit.GetService<IHandTrackingSimulationDataProvider>();
            dataProvider.Register(gameObject);
        }

        private void OnDestroy()
        {
            dataProvider.Unregister(gameObject);
            ClearJoints();
        }

        public void OnJointUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            if (eventData.Handedness != controllerVisualizer.Controller?.ControllerHandedness)
            {
                return;
            }

            MixedRealityHandControllerVisualizationProfile handControllerVisualizationProfile = profile;
            if (handControllerVisualizationProfile == null || !handControllerVisualizationProfile.EnableHandJointVisualization)
            {
                ClearJoints();
            }
            else
            {
                foreach (TrackedHandJoint handJoint in eventData.InputData.Keys)
                {
                    if (joints.TryGetValue(handJoint, out Transform jointTransform))
                    {
                        jointTransform.position = eventData.InputData[handJoint].Position;
                        jointTransform.rotation = eventData.InputData[handJoint].Rotation;
                    }
                    else
                    {
                        CreateJoint(handJoint, eventData);
                    }
                }
            }
        }

        private void ClearJoints()
        {
            foreach (var joint in joints)
            {
                Destroy(joint.Value.gameObject);
            }

            joints.Clear();
        }

        private void CreateJoint(TrackedHandJoint handJoint, InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            GameObject prefab = profile.JointPrefab;
            if (handJoint == TrackedHandJoint.Palm)
            {
                prefab = profile.PalmJointPrefab;
            }
            else if (handJoint == TrackedHandJoint.IndexTip)
            {
                prefab = profile.FingerTipPrefab;
            }

            GameObject jointObject;
            if (prefab != null)
            {
                jointObject = Instantiate(prefab);
            }
            else
            {
                jointObject = new GameObject();
            }

            jointObject.name = handJoint.ToString() + " Proxy Transform";
            jointObject.transform.position = eventData.InputData[handJoint].Position;
            jointObject.transform.rotation = eventData.InputData[handJoint].Rotation;
            jointObject.transform.parent = transform;

            joints.Add(handJoint, jointObject.transform);
        }
    }
}