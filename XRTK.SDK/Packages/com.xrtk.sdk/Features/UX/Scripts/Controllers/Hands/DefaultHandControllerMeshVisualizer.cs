// Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// <summary>
    /// Default hand controller visualizer for hand meshes.
    /// </summary>
    [RequireComponent(typeof(DefaultMixedRealityControllerVisualizer))]
    public class DefaultHandControllerMeshVisualizer : MonoBehaviour, IMixedRealityHandMeshHandler
    {
        private DefaultMixedRealityControllerVisualizer controllerVisualizer;
        private IMixedRealityHandControllerDataProvider dataProvider;

        /// <summary>
        /// The currently active hand visualization profile.
        /// </summary>
        private MixedRealityHandControllerVisualizationProfile profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        /// <summary>
        /// Provides access to the hand mesh used for visualization.
        /// </summary>
        public MeshFilter Mesh { get; private set; }

        private void Start()
        {
            controllerVisualizer = GetComponent<DefaultMixedRealityControllerVisualizer>();
            dataProvider = MixedRealityToolkit.GetService<IMixedRealityHandControllerDataProvider>();
            dataProvider.Register(this);
        }

        private void OnDestroy()
        {
            dataProvider.Unregister(this);
            ClearMesh();
        }

        public void OnMeshUpdated(InputEventData<HandMeshUpdatedEventData> eventData)
        {
            if (eventData.Handedness != controllerVisualizer.Controller?.ControllerHandedness)
            {
                return;
            }

            MixedRealityHandControllerVisualizationProfile handControllerVisualizationProfile = profile;
            if (handControllerVisualizationProfile == null || !handControllerVisualizationProfile.EnableHandMeshVisualization)
            {
                ClearMesh();
                return;
            }

            if (Mesh == null && profile?.HandMeshPrefab != null)
            {
                CreateMesh();
            }

            if (Mesh != null)
            {
                Mesh mesh = Mesh.mesh;

                mesh.vertices = eventData.InputData.Vertices;
                mesh.normals = eventData.InputData.Normals;
                mesh.triangles = eventData.InputData.Triangles;

                if (eventData.InputData.Uvs != null && eventData.InputData.Uvs.Length > 0)
                {
                    mesh.uv = eventData.InputData.Uvs;
                }

                Mesh.transform.position = eventData.InputData.Position;
                Mesh.transform.rotation = eventData.InputData.Rotation;
            }
        }

        private void ClearMesh()
        {
            if (Mesh != null)
            {
                Destroy(Mesh.gameObject);
            }
        }

        private void CreateMesh()
        {
            Mesh = Instantiate(profile.HandMeshPrefab).GetComponent<MeshFilter>();
        }
    }
}