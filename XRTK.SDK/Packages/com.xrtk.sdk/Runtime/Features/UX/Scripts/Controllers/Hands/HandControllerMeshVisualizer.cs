// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public class HandControllerMeshVisualizer : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter meshFilter = null;

        /// <summary>
        /// Updates the mesh visuailzation using latest hand data.
        /// </summary>
        /// <param name="handData">New hand data.</param>
        public void UpdateVisualization(HandData handData)
        {
            var handMeshData = handData.Mesh;
            if (handMeshData.Empty)
            {
                return;
            }

            if (meshFilter != null)
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
    }
}