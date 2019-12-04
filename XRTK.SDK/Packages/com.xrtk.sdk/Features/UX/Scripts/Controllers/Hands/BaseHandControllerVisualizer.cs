// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public abstract class BaseHandControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer, IMixedRealityHandDataHandler
    {
        private IMixedRealityHandControllerDataProvider dataProvider;
        private Dictionary<TrackedHandJoint, Transform> jointTransforms = new Dictionary<TrackedHandJoint, Transform>();

        [SerializeField]
        [Tooltip("Should a gizmo be drawn to represent the hand bounds.")]
        private bool drawBoundsGizmos = true;

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

        /// <summary>
        /// The mesh filter used for visualization.
        /// </summary>
        protected MeshFilter MeshFilter { get; private set; }

        /// <summary>
        /// Provides read-only access to the joint transforms used for visualization.
        /// </summary>
        public IReadOnlyDictionary<TrackedHandJoint, Transform> JointTransforms => jointTransforms;

        /// <summary>
        /// Should a gizmo be drawn to represent the hand bounds.
        /// </summary>
        public bool DrawBoundsGizmos
        {
            get { return drawBoundsGizmos; }
            set { drawBoundsGizmos = value; }
        }

        /// <summary>
        /// The currently active hand visualization profile.
        /// </summary>
        protected MixedRealityHandControllerVisualizationProfile Profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        /// <summary>
        /// Executes when the visualizer is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            dataProvider = MixedRealityToolkit.GetService<IMixedRealityHandControllerDataProvider>();
            dataProvider.Register(this);
        }

        /// <summary>
        /// Called by the Unity runtime when gizmos should be drawn.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (drawBoundsGizmos)
            {
                foreach (int trackedHandBounds in Enum.GetValues(typeof(TrackedHandBounds)))
                {
                    foreach (var controller in dataProvider.ActiveControllers)
                    {
                        if (controller.ControllerHandedness == Handedness
                            && controller is IMixedRealityHandController handController
                            && handController.TryGetBounds((TrackedHandBounds)trackedHandBounds, out Bounds? bounds))
                        {
                            Gizmos.DrawWireCube(bounds.Value.center, bounds.Value.size);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Executes when the visuailzer is disabled.
        /// </summary>
        protected override void OnDisable()
        {
            dataProvider.Unregister(this);
            ClearJoints();
            ClearMesh();

            base.OnDisable();
        }

        /// <inheritdoc />
        public virtual void OnHandDataUpdated(InputEventData<HandData> eventData)
        {
            if (eventData.Handedness != Controller.ControllerHandedness)
            {
                return;
            }

            HandData handData = eventData.InputData;
            UpdateHandJointVisualization(handData);
            UpdateHandJointVisualization(handData);
        }

        protected virtual void UpdateHandJointVisualization(HandData handData)
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

        protected virtual void UpdateHansMeshVisualization(HandData handData)
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

        protected virtual void ClearJoints()
        {
            foreach (var joint in jointTransforms)
            {
                Destroy(joint.Value.gameObject);
            }

            jointTransforms.Clear();
        }

        protected virtual void CreateJoint(TrackedHandJoint handJoint, IReadOnlyDictionary<TrackedHandJoint, MixedRealityPose> jointPoses)
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

        /// <summary>
        /// Destroys the mesh filter used for hand controller mesh visualization.
        /// </summary>
        protected virtual void ClearMesh()
        {
            if (MeshFilter != null)
            {
                Destroy(MeshFilter.gameObject);
            }
        }

        /// <summary>
        /// Creates the mesh filter used for hand controller mesh visualization.
        /// </summary>
        protected virtual void CreateMeshFilter()
        {
            MeshFilter = Instantiate(Profile.HandMeshPrefab).GetComponent<MeshFilter>();
        }
    }
}