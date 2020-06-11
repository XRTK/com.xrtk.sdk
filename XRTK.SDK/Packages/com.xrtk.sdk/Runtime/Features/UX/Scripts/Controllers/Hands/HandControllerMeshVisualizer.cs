// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public class HandControllerMeshVisualizer : MonoBehaviour
    {
        private Vector3[] lastHandMeshVertices;

        [SerializeField]
        private MeshFilter meshFilter = null;

        /// <summary>
        /// Updates the mesh visuailzation using latest hand mesh data.
        /// </summary>
        /// <param name="handMeshData">New hand mesh data.</param>
        public void UpdateVisualization(HandMeshData handMeshData)
        {
            if (handMeshData.IsEmpty)
            {
                return;
            }

            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;
                if (lastHandMeshVertices == null)
                {
                    lastHandMeshVertices = mesh.vertices;
                }

                bool meshChanged = false;
                // On some platforms, mesh length counts may change as the hand mesh is updated.
                // In order to update the vertices when the array sizes change, the mesh
                // must be cleared per instructions here:
                // https://docs.unity3d.com/ScriptReference/Mesh.html
                if (lastHandMeshVertices != null &&
                    lastHandMeshVertices.Length != handMeshData.Vertices.Length)
                {
                    meshChanged = true;
                    mesh.Clear();
                }

                mesh.vertices = handMeshData.Vertices;
                mesh.normals = handMeshData.Normals;
                lastHandMeshVertices = mesh.vertices;

                if (meshChanged)
                {
                    mesh.triangles = handMeshData.Triangles;
                    if (handMeshData.Uvs != null && handMeshData.Uvs.Length > 0)
                    {
                        mesh.uv = handMeshData.Uvs;
                    }

                    mesh.RecalculateBounds();
                }

                //meshFilter.transform.position = handMeshData.Position;
                //meshFilter.transform.rotation = handData.RootPose.Rotation;
            }
        }
    }
}