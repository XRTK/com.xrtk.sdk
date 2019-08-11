// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;
using XRTK.Services;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Base (and default) hand controller visualizer for hand meshes.
    /// </summary>
    public class BaseHandControllerMeshVisualizer : ControllerPoseSynchronizer, IMixedRealitySourceStateHandler, IMixedRealityHandMeshHandler
    {
        private MixedRealityHandControllerVisualizationProfile profile => MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile;

        protected MeshFilter handMeshFilter;

        private void OnDestroy()
        {
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

        void IMixedRealityHandMeshHandler.OnMeshUpdated(InputEventData<HandMeshUpdatedEventData> eventData)
        {
            if (eventData.Handedness != Controller?.ControllerHandedness)
            {
                return;
            }

            MixedRealityHandControllerVisualizationProfile handControllerVisualizationProfile = profile;
            if (handControllerVisualizationProfile != null && !handControllerVisualizationProfile.EnableHandMeshVisualization)
            {
                // clear existing meshes
                if (handMeshFilter != null)
                {
                    Destroy(handMeshFilter.gameObject);
                }

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