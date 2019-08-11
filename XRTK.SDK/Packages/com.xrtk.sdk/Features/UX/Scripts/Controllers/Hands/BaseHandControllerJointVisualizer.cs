// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.SDK.Input.Handlers;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Base (and default) hand controller visualizer for hand joints.
    /// </summary>
    public class BaseHandControllerJointVisualizer : ControllerPoseSynchronizer, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        protected readonly Dictionary<TrackedHandJoint, Transform> joints = new Dictionary<TrackedHandJoint, Transform>();
        
        private MixedRealityHandControllerVisualizationProfile profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        private void OnDestroy()
        {
            foreach (var joint in joints)
            {
                Destroy(joint.Value.gameObject);
            }
        }

        void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData) { }

        void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
        {
            if (Controller?.InputSource.SourceId == eventData.SourceId)
            {
                Destroy(gameObject);
            }
        }

        void IMixedRealityHandJointHandler.OnJointUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            if (eventData.Handedness != Controller?.ControllerHandedness)
            {
                return;
            }

            MixedRealityHandControllerVisualizationProfile handControllerVisualizationProfile = profile;
            if (handControllerVisualizationProfile != null && !handControllerVisualizationProfile.EnableHandJointVisualization)
            {
                // clear existing joint GameObjects
                foreach (var joint in joints)
                {
                    Destroy(joint.Value.gameObject);
                }

                joints.Clear();
                return;
            }

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