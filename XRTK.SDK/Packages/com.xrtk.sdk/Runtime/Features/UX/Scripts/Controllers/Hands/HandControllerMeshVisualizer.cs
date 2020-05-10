// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers.Hands;
using XRTK.EventDatum.Input;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Hand controller visualizer visualizing hand mesh data.
    /// </summary>
    public class HandControllerMeshVisualizer : BaseHandControllerVisualizer
    {
        private MeshFilter meshFilter;

        [SerializeField]
        [Tooltip("If this is not null and hand system supports hand meshes, use this mesh to render hand mesh.")]
        private GameObject handMeshPrefab = null;

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            base.OnInputChanged(eventData);

            var handMeshData = eventData.InputData.Mesh;
            if (eventData.Handedness != Controller.ControllerHandedness || handMeshData.Empty)
            {
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

        private bool CreateMeshFilter()
        {
            if (handMeshPrefab != null)
            {
                meshFilter = Instantiate(handMeshPrefab, HandVisualizationGameObject.transform).GetComponent<MeshFilter>();
                return true;
            }

            Debug.LogError("Failed to create mesh filter for hand mesh visualization. No prefab assigned.");
            return false;
        }
    }
}