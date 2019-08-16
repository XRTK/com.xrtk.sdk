// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Physics;
using XRTK.Utilities.Lines.DataProviders;
using XRTK.Utilities.Lines.Renderers;

namespace XRTK.SDK.UX.Pointers
{
    /// <summary>
    /// A simple line pointer for drawing lines from the input source origin to the current pointer position.
    /// </summary>
    public class LinePointer : BaseControllerPointer
    {
        [SerializeField]
        protected Gradient LineColorSelected = new Gradient();

        [SerializeField]
        protected Gradient LineColorValid = new Gradient();

        [SerializeField]
        protected Gradient LineColorInvalid = new Gradient();

        [SerializeField]
        protected Gradient LineColorNoTarget = new Gradient();

        [SerializeField]
        protected Gradient LineColorLockFocus = new Gradient();

        [Range(2, 50)]
        [SerializeField]
        [Tooltip("This setting has a high performance cost. Values above 20 are not recommended.")]
        protected int LineCastResolution = 10;

        [SerializeField]
        private BaseMixedRealityLineDataProvider lineBase;

        /// <summary>
        /// The Line Data Provider driving this pointer.
        /// </summary>
        public BaseMixedRealityLineDataProvider LineBase => lineBase;

        [SerializeField]
        [Tooltip("If no line renderers are specified, this array will be auto-populated on startup.")]
        private BaseMixedRealityLineRenderer[] lineRenderers;

        /// <summary>
        /// The current line renderers that this pointer is utilizing.
        /// </summary>
        /// <remarks>
        /// If no line renderers are specified, this array will be auto-populated on startup.
        /// </remarks>
        public BaseMixedRealityLineRenderer[] LineRenderers
        {
            get => lineRenderers;
            set => lineRenderers = value;
        }

        private void CheckInitialization()
        {
            if (lineBase == null)
            {
                lineBase = GetComponent<BaseMixedRealityLineDataProvider>();
            }

            if (lineBase == null)
            {
                Debug.LogError($"No Mixed Reality Line Data Provider found on {gameObject.name}. Did you forget to add a Line Data provider?");
            }

            if (lineBase != null && (lineRenderers == null || lineRenderers.Length == 0))
            {
                lineRenderers = lineBase.GetComponentsInChildren<BaseMixedRealityLineRenderer>();
            }

            if (lineRenderers == null || lineRenderers.Length == 0)
            {
                Debug.LogError($"No Mixed Reality Line Renderers found on {gameObject.name}. Did you forget to add a Mixed Reality Line Renderer?");
            }
        }

        #region MonoBehaviour Implementation

        protected virtual void OnValidate()
        {
            CheckInitialization();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CheckInitialization();
        }

        #endregion MonoBehaviour Implementation

        #region IMixedRealityPointer Implementation

        /// <inheritdoc />
        public override void OnPreRaycast()
        {
            Debug.Assert(lineBase != null);

            lineBase.UpdateMatrix();

            if (RayStabilizer != null)
            {
                RayStabilizer.UpdateStability(Rays[0].Origin, Rays[0].Direction);
                Rays[0].CopyRay(RayStabilizer.StableRay, PointerExtent);
            }

            TryGetPointerPosition(out var pointerPosition);
            TryGetPointerRotation(out var pointerRotation);

            // Set our first and last points
            lineBase.FirstPoint = pointerPosition;

            if (IsFocusLocked && Result.CurrentPointerTarget != null)
            {
                lineBase.LastPoint = Result.EndPoint;
            }
            else
            {
                lineBase.LastPoint = pointerPosition + pointerRotation * (Vector3.forward * PointerExtent);
            }

            // Make sure our array will hold
            if (Rays == null || Rays.Length != LineCastResolution)
            {
                Rays = new RayStep[LineCastResolution];
            }

            var stepSize = 1f / Rays.Length;
            var lastPoint = lineBase.GetUnClampedPoint(0f);

            for (int i = 0; i < Rays.Length; i++)
            {
                var currentPoint = lineBase.GetUnClampedPoint(stepSize * (i + 1));
                Rays[i].UpdateRayStep(ref lastPoint, ref currentPoint);
                lastPoint = currentPoint;
            }
        }

        /// <inheritdoc />
        public override void OnPostRaycast()
        {
            base.OnPostRaycast();

            Gradient lineColor;

            if (!IsInteractionEnabled)
            {
                lineBase.enabled = false;
                BaseCursor?.SetVisibility(false);
                return;
            }

            lineBase.enabled = true;
            BaseCursor?.SetVisibility(true);

            // The distance the ray travels through the world before it hits something.
            // Measured in world-units (as opposed to normalized distance).
            float clearWorldLength;

            // Used to ensure the line doesn't extend beyond the cursor
            float cursorOffsetWorldLength = BaseCursor?.SurfaceCursorDistance ?? 0f;

            // If we hit something
            if (Result.CurrentPointerTarget != null)
            {
                clearWorldLength = Result.RayDistance;
                lineColor = IsSelectPressed ? LineColorSelected : LineColorValid;
            }
            else
            {
                clearWorldLength = PointerExtent;
                lineColor = IsSelectPressed ? LineColorSelected : LineColorNoTarget;
            }

            if (IsFocusLocked)
            {
                lineColor = LineColorLockFocus;
            }

            int maxClampLineSteps = LineCastResolution;

            for (var i = 0; i < lineRenderers.Length; i++)
            {
                var lineRenderer = lineRenderers[i];
                // Renderers are enabled by default if line is enabled
                lineRenderer.enabled = true;
                maxClampLineSteps = Mathf.Max(maxClampLineSteps, lineRenderer.LineStepCount);
                lineRenderer.LineColor = lineColor;
            }

            // If focus is locked, we're sticking to the target
            // So don't clamp the world length
            if (IsFocusLocked)
            {
                float cursorOffsetLocalLength = LineBase.GetNormalizedLengthFromWorldLength(cursorOffsetWorldLength);
                LineBase.LineEndClamp = 1 - cursorOffsetLocalLength;
            }
            else
            {
                // Otherwise clamp the line end by the clear distance
                float clearLocalLength = lineBase.GetNormalizedLengthFromWorldLength(clearWorldLength - cursorOffsetWorldLength, maxClampLineSteps);
                lineBase.LineEndClamp = clearLocalLength;
            }
        }

        #endregion IMixedRealityPointer Implementation
    }
}