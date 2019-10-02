// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Input;
using XRTK.Providers.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Default hand controller visualizer for hand meshes.
    /// </summary>
    [RequireComponent(typeof(DefaultMixedRealityControllerVisualizer))]
    public class DefaultHandControllerMeshVisualizer : BaseHandControllerMeshVisualizer
    {
        private DefaultMixedRealityControllerVisualizer controllerVisualizer;
        private MeshFilter meshFilter;

        protected override void Start()
        {
            base.Start();
            controllerVisualizer = GetComponent<DefaultMixedRealityControllerVisualizer>();
        }

        protected override void OnDestroy()
        {
            ClearMesh();
            base.OnDestroy();
        }

        public override void OnMeshUpdated(InputEventData<HandMeshData> eventData)
        {
            if (eventData.Handedness != controllerVisualizer.Controller?.ControllerHandedness)
            {
                return;
            }

            if (Profile == null || !Profile.EnableHandMeshVisualization)
            {
                ClearMesh();
                return;
            }

            if (meshFilter == null && Profile?.HandMeshPrefab != null)
            {
                CreateMesh();
            }

            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;

                mesh.vertices = eventData.InputData.Vertices;
                mesh.normals = eventData.InputData.Normals;
                mesh.triangles = eventData.InputData.Triangles;

                if (eventData.InputData.Uvs != null && eventData.InputData.Uvs.Length > 0)
                {
                    mesh.uv = eventData.InputData.Uvs;
                }

                meshFilter.transform.position = eventData.InputData.Position;
                meshFilter.transform.rotation = eventData.InputData.Rotation;
            }
        }

        private void ClearMesh()
        {
            if (meshFilter != null)
            {
                Destroy(meshFilter.gameObject);
            }
        }

        private void CreateMesh()
        {
            meshFilter = Instantiate(Profile.HandMeshPrefab).GetComponent<MeshFilter>();
        }
    }
}