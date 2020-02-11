// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.Extensions;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Hands;

#if UNITY_EDITOR
using XRTK.Utilities;
#endif

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Default hand controller visualizer implementation.
    /// </summary>
    public class DefaultHandControllerVisualizer : BaseHandControllerVisualizer
    {
        private Dictionary<TrackedHandJoint, Transform> jointTransforms = new Dictionary<TrackedHandJoint, Transform>();

        [Header("Joint Visualization Settings")]
        [SerializeField]
        [Tooltip("The joint prefab to use.")]
        private GameObject jointPrefab = null;

        [SerializeField]
        [Tooltip("The joint prefab to use for palm.")]
        private GameObject palmPrefab = null;

        [SerializeField]
        [Tooltip("The joint prefab to use for the index tip (point of interaction.")]
        private GameObject fingertipPrefab = null;

        [SerializeField]
        [Tooltip("Material tint color for index fingertip.")]
        private Color indexFingertipColor = Color.cyan;

        [Header("Mesh Visualization Settings")]
        [SerializeField]
        [Tooltip("If this is not null and hand system supports hand meshes, use this mesh to render hand mesh.")]
        private GameObject handMeshPrefab = null;

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
            ClearJoints();
            ClearMesh();

            base.OnDisable();
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            IMixedRealityHandController handController = Controller as IMixedRealityHandController;
            if (handController.TryGetBounds(TrackedHandBounds.Hand, out Bounds? handBounds))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(handBounds.Value.center, handBounds.Value.size);
            }

            if (handController.TryGetBounds(TrackedHandBounds.IndexFinger, out Bounds? indexFingerBounds))
            {
                Gizmos.color = Color.green;
                GizmoUtilities.DrawWireCapsule(indexFingerBounds.Value.center, Quaternion.identity, .1f, 1f);
            }
        }

#endif

        /// <inheritdoc />
        protected override void UpdateHandJointVisualization(HandData handData)
        {
            if (!EnableHandJointVisualization)
            {
                ClearJoints();
            }
            else
            {
                IReadOnlyDictionary<TrackedHandJoint, MixedRealityPose> jointPoses = handData.Joints.ToJointPoseDictionary();
                foreach (TrackedHandJoint handJoint in jointPoses.Keys)
                {
                    if (handJoint != TrackedHandJoint.None && TryGetOrCreateJoint(handJoint, out Transform jointTransform))
                    {
                        MixedRealityPose jointPose = jointPoses[handJoint];
                        jointTransform.localPosition = jointPose.Position;
                        jointTransform.localRotation = jointPose.Rotation;
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void UpdateHansMeshVisualization(HandData handData)
        {
            HandMeshData handMeshData = handData.Mesh;
            if (!EnableHandMeshVisualization || handMeshData == null || handMeshData.Empty)
            {
                ClearMesh();
                return;
            }

            if (MeshFilter != null || CreateMeshFilter())
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

        private void ClearJoints()
        {
            foreach (var joint in jointTransforms)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(joint.Value.gameObject);
                }
                else
                {
                    Destroy(joint.Value.gameObject);
                }
            }

            jointTransforms.Clear();
        }

        private bool TryGetOrCreateJoint(TrackedHandJoint handJoint, out Transform jointTransform)
        {
            if (jointTransforms.TryGetValue(handJoint, out Transform existingJointTransform))
            {
                jointTransform = existingJointTransform;
                return true;
            }

            GameObject prefab = jointPrefab;
            if (handJoint == TrackedHandJoint.Palm)
            {
                prefab = palmPrefab;
            }
            else if (handJoint == TrackedHandJoint.IndexTip || handJoint == TrackedHandJoint.MiddleTip
                || handJoint == TrackedHandJoint.PinkyTip || handJoint == TrackedHandJoint.RingTip || handJoint == TrackedHandJoint.ThumbTip)
            {
                prefab = fingertipPrefab;
            }

            if (prefab != null)
            {
                jointTransform = Instantiate(prefab).transform;
                jointTransform.name = $"{handJoint} Proxy Transform";
                jointTransform.parent = transform;
                jointTransforms.Add(handJoint, jointTransform.transform);

                if (handJoint == TrackedHandJoint.IndexTip)
                {
                    Renderer indexJointRenderer = jointTransform.GetComponent<Renderer>();
                    Material indexMaterial = indexJointRenderer.material;
                    indexMaterial.color = indexFingertipColor;
                    indexJointRenderer.material = indexMaterial;
                }

                return true;
            }

            Debug.LogError($"Failed to create {handJoint} game object for hand joint visualization. Prefab not assigned.");
            jointTransform = null;
            return false;
        }

        private void ClearMesh()
        {
            if (MeshFilter != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(MeshFilter.gameObject);
                }
                else
                {
                    Destroy(MeshFilter.gameObject);
                }
            }
        }

        private bool CreateMeshFilter()
        {
            if (handMeshPrefab != null)
            {
                MeshFilter = Instantiate(handMeshPrefab).GetComponent<MeshFilter>();
                return true;
            }

            Debug.LogError($"Failed to create mesh filter for hand mesh visualization. No prefab assigned.");
            return false;
        }
    }
}