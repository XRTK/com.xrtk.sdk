// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.Serialization;
using XRTK.Definitions.InputSystem;
using XRTK.Interfaces.CameraSystem;
using XRTK.Services;
using XRTK.Utilities;

namespace XRTK.SDK.UX.Cursors
{
    /// <summary>
    /// Object that represents a cursor in 3D space controlled by gaze.
    /// </summary>
    public class MeshCursor : BaseCursor
    {
        [Serializable]
        public struct MeshCursorDatum
        {
            public string Name;
            public CursorStateEnum CursorState;
            public Mesh CursorMesh;
            public Vector3 LocalScale;
            public Vector3 LocalOffset;
        }

        [SerializeField]
        public MeshCursorDatum[] CursorStateData;

        /// <summary>
        /// Sprite renderer to change.  If null find one in children
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("TargetRenderer")]
        private MeshRenderer targetRenderer;

        [SerializeField]
        private float fixedSize = 1f;

        [SerializeField]
        private Vector3 fixedSizeOffset = Vector3.zero;

        /// <summary>
        /// On enable look for a sprite renderer on children
        /// </summary>
        protected override void OnEnable()
        {
            if (CursorStateData == null)
            {
                CursorStateData = new MeshCursorDatum[0];
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<MeshRenderer>();
            }

            base.OnEnable();
        }

        private void LateUpdate()
        {
            if (targetRenderer == null) { return; }

            var targetTransform = targetRenderer.transform;
            var targetCamera = MixedRealityToolkit.TryGetSystem<IMixedRealityCameraSystem>(out var cameraSystem)
                ? cameraSystem.MainCameraRig.PlayerCamera
                : CameraCache.Main;

            var cameraPosition = targetCamera.transform.position;
            var distance = (cameraPosition - targetTransform.position).magnitude;
            var size = distance * fixedSize * targetCamera.fieldOfView;

            targetTransform.localScale = Vector3.one * size;
            targetTransform.localPosition = new Vector3(fixedSizeOffset.x * size, fixedSizeOffset.y * size, fixedSizeOffset.z * size);
        }

        /// <summary>
        /// Override OnCursorState change to set the correct animation
        /// state for the cursor
        /// </summary>
        /// <param name="state"></param>
        public override void OnCursorStateChange(CursorStateEnum state)
        {
            base.OnCursorStateChange(state);

            if (state != CursorStateEnum.Contextual)
            {
                for (int i = 0; i < CursorStateData.Length; i++)
                {
                    if (CursorStateData[i].CursorState == state)
                    {
                        SetCursorState(CursorStateData[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Based on the type of state info pass it through to the mesh renderer
        /// </summary>
        /// <param name="stateDatum"></param>
        private void SetCursorState(MeshCursorDatum stateDatum)
        {
            // Return if we do not have an animator
            if (targetRenderer != null)
            {
                var filter = targetRenderer.gameObject.GetComponent<MeshFilter>();
                if (filter != null && stateDatum.CursorMesh != null)
                {
                    filter.mesh = stateDatum.CursorMesh;
                }

                var targetTransform = targetRenderer.transform;
                targetTransform.localPosition = stateDatum.LocalOffset;
                targetTransform.localScale = stateDatum.LocalScale;
            }
        }
    }
}
