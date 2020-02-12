// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;

#if UNITY_EDITOR
using XRTK.Utilities;
#endif

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Default hand controller visualizer implementation.
    /// </summary>
    public class DefaultHandControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer
    {
        private readonly Dictionary<TrackedHandJoint, Transform> jointTransforms = new Dictionary<TrackedHandJoint, Transform>();
        private MeshFilter meshFilter;
        private const float fingerColliderRadius = .007f;
        private const int capsuleColliderZAxis = 2;
        private GameObject root;

        [SerializeField]
        [Tooltip("Renders the hand joints. Note: this could reduce performance.")]
        private bool enableHandJointVisualization = true;

        [SerializeField]
        [Tooltip("Renders the hand mesh, if available. Note: this could reduce performance.")]
        private bool enableHandMeshVisualization = false;

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
        /// Is hand joint rendering enabled?
        /// </summary>
        protected bool EnableHandJointVisualization => enableHandJointVisualization;

        /// <summary>
        /// Is hand mesh rendering enabled?
        /// </summary>
        protected bool EnableHandMeshVisualization => enableHandMeshVisualization;

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

        public GameObject PhysicsProxy { get; private set; }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            ClearJointsVisualization();
            ClearMeshVisualization();
            ClearPhysics();

            base.OnDisable();
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            //IMixedRealityHandController handController = Controller as IMixedRealityHandController;
            //if (handController.TryGetBounds(TrackedHandBounds.Hand, out HandBounds handBounds))
            //{
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawWireCube(handBounds.Bounds.center, handBounds.Bounds.size);
            //}

            //if (handController.TryGetBounds(TrackedHandBounds.IndexFinger, out HandBounds indexFingerBounds))
            //{
            //    Gizmos.color = Color.green;
            //    GizmoUtilities.DrawWireCapsule(indexFingerBounds.Bounds.center, indexFingerBounds.Rotation, .01f, indexFingerBounds.Bounds.size.magnitude);
            //}
        }

#endif

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            if (eventData.Handedness != Controller.ControllerHandedness)
            {
                return;
            }

            VerifyPhysicsConfiguration();
            HandData handData = eventData.InputData;
            UpdateHandJointVisualization(handData);
            UpdateHansMeshVisualization(handData);
        }

        private void UpdateHandJointVisualization(HandData handData)
        {
            if (!EnableHandJointVisualization)
            {
                ClearJointsVisualization();
            }
            else
            {
                IReadOnlyDictionary<TrackedHandJoint, MixedRealityPose> jointPoses = handData.Joints.ToJointPoseDictionary();
                foreach (TrackedHandJoint handJoint in jointPoses.Keys)
                {
                    if (handJoint != TrackedHandJoint.None)
                    {
                        Transform jointTransform = GetOrCreateJoint(handJoint);
                        MixedRealityPose jointPose = jointPoses[handJoint];
                        jointTransform.localPosition = jointPose.Position;
                        jointTransform.localRotation = jointPose.Rotation;
                    }
                }
            }

            UpdateHandColliders();
        }

        private void UpdateHansMeshVisualization(HandData handData)
        {
            HandMeshData handMeshData = handData.Mesh;
            if (!EnableHandMeshVisualization || handMeshData == null || handMeshData.Empty)
            {
                ClearMeshVisualization();
                return;
            }

            if (meshFilter != null || CreateMeshFilter())
            {
                Mesh mesh = meshFilter.mesh;

                mesh.vertices = handMeshData.Vertices;
                mesh.normals = handMeshData.Normals;
                mesh.triangles = handMeshData.Triangles;

                if (handMeshData.Uvs != null && handMeshData.Uvs.Length > 0)
                {
                    mesh.uv = handMeshData.Uvs;
                }

                meshFilter.transform.position = handMeshData.Position;
                meshFilter.transform.rotation = handMeshData.Rotation;
            }
        }

        #region Hand Colliders / Physics

        private void VerifyPhysicsConfiguration()
        {
            if (PhysicsProxy != null)
            {
                return;
            }

            PhysicsProxy = new GameObject($"{GameObjectProxy.name}_PhysicsProxy");
            PhysicsProxy.transform.parent = GameObjectProxy.transform.parent;

            // Setup the kinematic rigidbody on the actual controller game object.
            Rigidbody controllerRigidbody = GameObjectProxy.GetOrAddComponent<Rigidbody>();
            controllerRigidbody.isKinematic = true;
            controllerRigidbody.useGravity = false;

            // Make the physics proxy a fixed joint rigidbody to the controller.
            Rigidbody physicsRigidbody = PhysicsProxy.GetOrAddComponent<Rigidbody>();
            physicsRigidbody.mass = float.MaxValue;
            FixedJoint fixedJoint = PhysicsProxy.GetOrAddComponent<FixedJoint>();
            fixedJoint.connectedBody = controllerRigidbody;
            fixedJoint.breakForce = float.MaxValue;
            fixedJoint.breakTorque = float.MaxValue;

            // TODO: Only do this if hand physics enabled.
            root = PhysicsProxy;
        }

        private void UpdateHandColliders()
        {
            IMixedRealityHandController handController = Controller as IMixedRealityHandController;

            if (handController.TryGetBounds(TrackedHandBounds.Thumb, out Bounds[] thumbBounds))
            {
                // Thumb bounds are made up of two capsule collider bounds entries.
                Bounds knuckleToMiddle = thumbBounds[0];
                Bounds middleToTip = thumbBounds[1];

                GameObject thumbKnuckleGameObject = GetOrCreateJoint(TrackedHandJoint.ThumbMetacarpalJoint).gameObject;
                CapsuleCollider capsuleCollider = thumbKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, thumbKnuckleGameObject.transform);

                GameObject thumbMiddleGameObject = GetOrCreateJoint(TrackedHandJoint.ThumbProximalJoint).gameObject;
                capsuleCollider = thumbMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, middleToTip, thumbMiddleGameObject.transform);
            }

            if (handController.TryGetBounds(TrackedHandBounds.IndexFinger, out Bounds[] indexFingerBounds))
            {
                // Index finger bounds are made up of two capsule collider bounds entries.
                Bounds knuckleToMiddle = indexFingerBounds[0];
                Bounds middleToTip = indexFingerBounds[1];

                GameObject indexKnuckleGameObject = GetOrCreateJoint(TrackedHandJoint.IndexKnuckle).gameObject;
                CapsuleCollider capsuleCollider = indexKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, indexKnuckleGameObject.transform);

                GameObject indexMiddleGameObject = GetOrCreateJoint(TrackedHandJoint.IndexMiddleJoint).gameObject;
                capsuleCollider = indexMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, middleToTip, indexMiddleGameObject.transform);
            }

            if (handController.TryGetBounds(TrackedHandBounds.MiddleFinger, out Bounds[] middleFingerBounds))
            {
                // Middle finger bounds are made up of two capsule collider bounds entries.
                Bounds knuckleToMiddle = middleFingerBounds[0];
                Bounds middleToTip = middleFingerBounds[1];

                GameObject middleKnuckleGameObject = GetOrCreateJoint(TrackedHandJoint.MiddleKnuckle).gameObject;
                CapsuleCollider capsuleCollider = middleKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, middleKnuckleGameObject.transform);

                GameObject middleMiddleGameObject = GetOrCreateJoint(TrackedHandJoint.MiddleMiddleJoint).gameObject;
                capsuleCollider = middleMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, middleToTip, middleMiddleGameObject.transform);
            }

            if (handController.TryGetBounds(TrackedHandBounds.RingFinger, out Bounds[] ringFingerBounds))
            {
                // Ring finger bounds are made up of two capsule collider bounds entries.
                Bounds knuckleToMiddle = ringFingerBounds[0];
                Bounds middleToTip = ringFingerBounds[1];

                GameObject ringKnuckleGameObject = GetOrCreateJoint(TrackedHandJoint.RingKnuckle).gameObject;
                CapsuleCollider capsuleCollider = ringKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, ringKnuckleGameObject.transform);

                GameObject ringMiddleGameObject = GetOrCreateJoint(TrackedHandJoint.RingMiddleJoint).gameObject;
                capsuleCollider = ringMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, middleToTip, ringMiddleGameObject.transform);
            }

            if (handController.TryGetBounds(TrackedHandBounds.Pinky, out Bounds[] pinkyFingerBounds))
            {
                // Pinky finger bounds are made up of two capsule collider bounds entries.
                Bounds knuckleToMiddle = pinkyFingerBounds[0];
                Bounds middleToTip = pinkyFingerBounds[1];

                GameObject pinkyKnuckleGameObject = GetOrCreateJoint(TrackedHandJoint.PinkyKnuckle).gameObject;
                CapsuleCollider capsuleCollider = pinkyKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, pinkyKnuckleGameObject.transform);

                GameObject pinkyMiddleGameObject = GetOrCreateJoint(TrackedHandJoint.PinkyMiddleJoint).gameObject;
                capsuleCollider = pinkyMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, middleToTip, pinkyMiddleGameObject.transform);
            }

            if (handController.TryGetBounds(TrackedHandBounds.Palm, out Bounds[] palmBounds))
            {
                // For the palm we create a composite collider using a capsule collider per
                // finger for the area metacarpal <-> knuckle.
                Bounds indexPalmBounds = palmBounds[0];
                GameObject indexMetacarpalGameObject = GetOrCreateJoint(TrackedHandJoint.IndexMetacarpal).gameObject;
                CapsuleCollider capsuleCollider = indexMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, indexPalmBounds, indexMetacarpalGameObject.transform);

                Bounds middlePalmBounds = palmBounds[1];
                GameObject middleMetacarpalGameObject = GetOrCreateJoint(TrackedHandJoint.MiddleMetacarpal).gameObject;
                capsuleCollider = middleMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, middlePalmBounds, middleMetacarpalGameObject.transform);

                Bounds ringPalmBounds = palmBounds[2];
                GameObject ringMetacarpalGameObject = GetOrCreateJoint(TrackedHandJoint.RingMetacarpal).gameObject;
                capsuleCollider = ringMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, ringPalmBounds, ringMetacarpalGameObject.transform);

                Bounds pinkyPalmBounds = palmBounds[3];
                GameObject pinkyMetacarpalGameObject = GetOrCreateJoint(TrackedHandJoint.PinkyMetacarpal).gameObject;
                capsuleCollider = pinkyMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                ConfigureCapsuleCollider(capsuleCollider, pinkyPalmBounds, pinkyMetacarpalGameObject.transform);
            }

            //if (handController.TryGetBounds(TrackedHandBounds.Hand, out Bounds[] handBounds))
            //{
            //    // For full hand bounds we'll only get one bounds entry, which is a box
            //    // encapsulating the whole hand.
            //    Bounds fullHandBounds = handBounds[0];
            //    BoxCollider boxCollider = root.GetOrAddComponent<BoxCollider>();
            //    boxCollider.center = fullHandBounds.center;
            //    boxCollider.size = fullHandBounds.size;
            //}
        }

        private void ConfigureCapsuleCollider(CapsuleCollider collider, Bounds bounds, Transform jointTransform)
        {
            collider.radius = fingerColliderRadius;
            collider.direction = capsuleColliderZAxis;
            collider.height = bounds.size.magnitude;
            collider.center = jointTransform.transform.InverseTransformPoint(bounds.center);
        }

        #endregion

        /// <summary>
        /// Clears any created joint visualization objects by destorying
        /// all child transforms of a given joint transform.
        /// </summary>
        private void ClearJointsVisualization()
        {
            foreach (var joint in jointTransforms)
            {
                // We want to keep the joint transform here,
                // it's not used for visualization and might still be needed
                // for physics etc. Just delete child transforms, which were
                // instantiated from visualization prefabs.
                while (joint.Value.childCount > 0)
                {
                    if (Application.isEditor)
                    {
                        DestroyImmediate(joint.Value.GetChild(0).gameObject);
                    }
                    else
                    {
                        Destroy(joint.Value.GetChild(0).gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Clears any existing hand mesh visualzation.
        /// </summary>
        private void ClearMeshVisualization()
        {
            if (meshFilter != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(meshFilter.gameObject);
                }
                else
                {
                    Destroy(meshFilter.gameObject);
                }
            }
        }

        /// <summary>
        /// Clears any physics related resources.
        /// </summary>
        private void ClearPhysics()
        {
            if (PhysicsProxy != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(PhysicsProxy);
                }
                else
                {
                    Destroy(PhysicsProxy);
                }
            }
        }

        private Transform GetOrCreateJoint(TrackedHandJoint handJoint)
        {
            if (jointTransforms.TryGetValue(handJoint, out Transform existingJointTransform))
            {
                return existingJointTransform;
            }

            Transform jointTransform = new GameObject($"{handJoint} Proxy Transform").transform;
            jointTransform.parent = root.transform;
            jointTransforms.Add(handJoint, jointTransform.transform);

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
                GameObject jointVisualization = Instantiate(prefab, jointTransform);
                if (handJoint == TrackedHandJoint.IndexTip)
                {
                    Renderer indexJointRenderer = jointVisualization.GetComponent<Renderer>();
                    Material indexMaterial = indexJointRenderer.material;
                    indexMaterial.color = indexFingertipColor;
                    indexJointRenderer.material = indexMaterial;
                }
            }

            return jointTransform;
        }

        private bool CreateMeshFilter()
        {
            if (handMeshPrefab != null)
            {
                meshFilter = Instantiate(handMeshPrefab).GetComponent<MeshFilter>();
                meshFilter.transform.parent = root.transform;
                return true;
            }

            Debug.LogError($"Failed to create mesh filter for hand mesh visualization. No prefab assigned.");
            return false;
        }
    }
}