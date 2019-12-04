// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Default hand controller visualizer implementation.
    /// </summary>
    public class DefaultHandControllerVisualizer : BaseHandControllerVisualizer
    {
        private Dictionary<TrackedHandJoint, Transform> jointTransforms = new Dictionary<TrackedHandJoint, Transform>();

        /// <summary>
        /// The mesh filter used for visualization.
        /// </summary>
        public MeshFilter MeshFilter { get; private set; }

        /// <summary>
        /// Provides read-only access to the joint transforms used for visualization.
        /// </summary>
        public IReadOnlyDictionary<TrackedHandJoint, Transform> JointTransforms => jointTransforms;

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();

            ClearJoints();
            ClearMesh();
        }

        /// <inheritdoc />
        protected override void UpdateHandJointVisualization(HandData handData)
        {
            if (Profile == null || !Profile.EnableHandJointVisualization)
            {
                ClearJoints();
            }
            else
            {
                IReadOnlyDictionary<TrackedHandJoint, MixedRealityPose> jointPoses = HandUtils.ToJointPoseDictionary(handData.Joints);
                foreach (TrackedHandJoint handJoint in jointPoses.Keys)
                {
                    if (jointTransforms.TryGetValue(handJoint, out Transform jointTransform))
                    {
                        jointTransform.position = jointPoses[handJoint].Position;
                        jointTransform.rotation = jointPoses[handJoint].Rotation;
                    }
                    else if (handJoint != TrackedHandJoint.None)
                    {
                        CreateJoint(handJoint, jointPoses);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void UpdateHansMeshVisualization(HandData handData)
        {
            if (Profile == null || !Profile.EnableHandMeshVisualization || handData.Mesh == null)
            {
                ClearMesh();
                return;
            }

            HandMeshData handMeshData = handData.Mesh;
            if (handMeshData.Empty)
            {
                return;
            }

            if (MeshFilter == null && Profile?.HandMeshPrefab != null)
            {
                CreateMeshFilter();
            }

            if (MeshFilter != null)
            {
                Mesh mesh = MeshFilter.mesh;

                mesh.vertices = handMeshData.Vertices;
                mesh.normals = handMeshData.Normals;
                mesh.triangles = handMeshData.Triangles;

                if (handMeshData.Uvs != null && handMeshData.Uvs.Length > 0)
                {
                    mesh.uv = handMeshData.Uvs;
                }

                MeshFilter.transform.position = handMeshData.Position;
                MeshFilter.transform.rotation = handMeshData.Rotation;
            }
        }

        private void CreateJoint(TrackedHandJoint handJoint, IReadOnlyDictionary<TrackedHandJoint, MixedRealityPose> jointPoses)
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
            jointObject.transform.position = jointPoses[handJoint].Position;
            jointObject.transform.rotation = jointPoses[handJoint].Rotation;
            jointObject.transform.parent = transform;

            jointTransforms.Add(handJoint, jointObject.transform);
        }

        private void ClearJoints()
        {
            foreach (var joint in jointTransforms)
            {
                Destroy(joint.Value.gameObject);
            }

            jointTransforms.Clear();
        }

        private void ClearMesh()
        {
            if (MeshFilter != null)
            {
                Destroy(MeshFilter.gameObject);
            }
        }

        private void CreateMeshFilter()
        {
            MeshFilter = Instantiate(Profile.HandMeshPrefab).GetComponent<MeshFilter>();
        }
    }
}