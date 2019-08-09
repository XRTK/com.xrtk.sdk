// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public class BaseHandVisualizer : ControllerPoseSynchronizer, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler, IMixedRealityHandMeshHandler
    {
        protected readonly Dictionary<TrackedHandJoint, Transform> joints = new Dictionary<TrackedHandJoint, Transform>();
        protected MeshFilter handMeshFilter;

        private MixedRealityHandControllerVisualizationProfile profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        private void OnDestroy()
        {
            foreach (var joint in joints)
            {
                Destroy(joint.Value.gameObject);
            }

            if (handMeshFilter != null)
            {
                Destroy(handMeshFilter.gameObject);
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

            MixedRealityHandControllerVisualizationProfile handTrackingProfile = profile;
            if (handTrackingProfile != null && !handTrackingProfile.EnableHandJointVisualization)
            {
                // clear existing joint GameObjects / meshes
                foreach (var joint in joints)
                {
                    Destroy(joint.Value.gameObject);
                }

                joints.Clear();
                return;
            }

            foreach (TrackedHandJoint handJoint in eventData.InputData.Keys)
            {
                Transform jointTransform;
                if (joints.TryGetValue(handJoint, out jointTransform))
                {
                    jointTransform.position = eventData.InputData[handJoint].Position;
                    jointTransform.rotation = eventData.InputData[handJoint].Rotation;
                }
                else
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

        void IMixedRealityHandMeshHandler.OnMeshUpdated(InputEventData<HandMeshUpdatedEventData> eventData)
        {
            if (eventData.Handedness != Controller?.ControllerHandedness)
            {
                return;
            }

            if (handMeshFilter == null && profile?.HandMeshPrefab != null)
            {
                handMeshFilter = Instantiate(profile.HandMeshPrefab).GetComponent<MeshFilter>();
            }

            if (handMeshFilter != null)
            {
                Mesh mesh = handMeshFilter.mesh;

                mesh.vertices = eventData.InputData.Vertices;
                mesh.normals = eventData.InputData.Normals;
                mesh.triangles = eventData.InputData.Triangles;

                if (eventData.InputData.Uvs != null && eventData.InputData.Uvs.Length > 0)
                {
                    mesh.uv = eventData.InputData.Uvs;
                }

                handMeshFilter.transform.position = eventData.InputData.Position;
                handMeshFilter.transform.rotation = eventData.InputData.Rotation;
            }
        }
    }
}