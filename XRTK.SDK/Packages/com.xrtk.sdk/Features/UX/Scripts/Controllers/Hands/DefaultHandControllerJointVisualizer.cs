// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Default hand controller visualizer for hand joints.
    /// </summary>
    [RequireComponent(typeof(DefaultMixedRealityControllerVisualizer))]
    public class DefaultHandControllerJointVisualizer : BaseHandControllerJointVisualizer
    {
        private DefaultMixedRealityControllerVisualizer controllerVisualizer;
        private Dictionary<TrackedHandJoint, Transform> jointTransforms;

        /// <summary>
        /// Provides read-only access to the joint transforms used for visualization.
        /// </summary>
        public IReadOnlyDictionary<TrackedHandJoint, Transform> JointTransforms => jointTransforms;

        protected override void Start()
        {
            base.Start();

            jointTransforms = new Dictionary<TrackedHandJoint, Transform>();
            controllerVisualizer = GetComponent<DefaultMixedRealityControllerVisualizer>();
        }

        protected override void OnDestroy()
        {
            ClearJoints();
            base.OnDestroy();
        }

        public override void OnJointUpdated(InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            if (eventData.Handedness != controllerVisualizer.Controller?.ControllerHandedness)
            {
                return;
            }

            if (Profile == null || !Profile.EnableHandJointVisualization)
            {
                ClearJoints();
            }
            else
            {
                foreach (TrackedHandJoint handJoint in eventData.InputData.Keys)
                {
                    if (jointTransforms.TryGetValue(handJoint, out Transform jointTransform))
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
            foreach (var joint in jointTransforms)
            {
                Destroy(joint.Value.gameObject);
            }

            jointTransforms.Clear();
        }

        private void CreateJoint(TrackedHandJoint handJoint, InputEventData<IDictionary<TrackedHandJoint, MixedRealityPose>> eventData)
        {
            GameObject prefab = Profile.JointPrefab;
            if (handJoint == TrackedHandJoint.Palm)
            {
                prefab = Profile.PalmJointPrefab;
            }
            else if (handJoint == TrackedHandJoint.IndexTip)
            {
                prefab = Profile.FingerTipPrefab;
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

            jointTransforms.Add(handJoint, jointObject.transform);
        }
    }
}