// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using XRTK.Attributes;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.SDK.Input.Handlers;
using XRTK.Services;

namespace XRTK.SDK.UX
{
    public class BoundingBox : MonoBehaviour
    {
        private class BoundingBoxRig : BaseInputHandler,
            IMixedRealitySourceStateHandler,
            IMixedRealityPointerHandler,
            IMixedRealityInputHandler<MixedRealityPose>
        {
            public override bool IsFocusRequired => false;

            public BoundingBox BoundingBoxParent
            {
                get
                {
                    if (boundingBoxParent == null)
                    {
                        Destroy(gameObject);
                    }

                    return boundingBoxParent;
                }
                set => boundingBoxParent = value;
            }

            private BoundingBox boundingBoxParent;

            /// <inheritdoc />
            void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
            {
                if (BoundingBoxParent.currentInputSource != null) { return; }

                var pointer = eventData.Pointer;

                if (pointer.TryGetPointingRay(out var ray))
                {
                    BoundingBoxParent.handleMoveType = HandleMoveType.Ray;
                    var grabbedCollider = BoundingBoxParent.GetGrabbedCollider(ray, out var distance);

                    if (grabbedCollider == null) { return; }

                    MixedRealityToolkit.InputSystem.PushModalInputHandler(gameObject);

                    BoundingBoxParent.currentInputSource = eventData.InputSource;
                    BoundingBoxParent.currentPointer = pointer;
                    BoundingBoxParent.grabbedHandle = grabbedCollider.gameObject;
                    BoundingBoxParent.currentHandleType = BoundingBoxParent.GetHandleType(BoundingBoxParent.grabbedHandle);
                    BoundingBoxParent.currentRotationAxis = BoundingBoxParent.GetRotationAxis(BoundingBoxParent.grabbedHandle);
                    BoundingBoxParent.currentPointer.TryGetPointingRay(out _);
                    BoundingBoxParent.initialGrabMag = distance;
                    BoundingBoxParent.initialGrabbedPosition = BoundingBoxParent.grabbedHandle.transform.position;
                    BoundingBoxParent.initialScale = transform.localScale;
                    pointer.TryGetPointerPosition(out BoundingBoxParent.initialGrabPoint);
                    BoundingBoxParent.ShowOneHandle(BoundingBoxParent.grabbedHandle);
                    BoundingBoxParent.initialGazePoint = Vector3.zero;
                    eventData.Use();
                }
            }

            /// <inheritdoc />
            void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
            {
                if (BoundingBoxParent.currentInputSource != null &&
                    eventData.InputSource.SourceId == BoundingBoxParent.currentInputSource.SourceId)
                {
                    BoundingBoxParent.currentInputSource = null;
                    BoundingBoxParent.currentHandleType = HandleType.None;
                    BoundingBoxParent.currentPointer = null;
                    BoundingBoxParent.grabbedHandle = null;
                    BoundingBoxParent.ResetHandleVisibility();
                    eventData.Use();
                    MixedRealityToolkit.InputSystem.PopModalInputHandler();
                }
            }

            /// <inheritdoc />
            void IMixedRealityInputHandler<MixedRealityPose>.OnInputChanged(InputEventData<MixedRealityPose> eventData)
            {
                if (BoundingBoxParent.currentInputSource != null &&
                    eventData.InputSource.SourceId == BoundingBoxParent.currentInputSource.SourceId)
                {
                    var point = eventData.InputData.Position;
                    BoundingBoxParent.usingPose = true;

                    if (BoundingBoxParent.initialGazePoint == Vector3.zero)
                    {
                        BoundingBoxParent.initialGazePoint = point;
                    }

                    BoundingBoxParent.currentPosePosition = BoundingBoxParent.initialGrabbedPosition + (point - BoundingBoxParent.initialGazePoint);
                }
                else
                {
                    BoundingBoxParent.usingPose = false;
                }
            }

            /// <inheritdoc />
            void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData) { }

            /// <inheritdoc />
            void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
            {
                if (BoundingBoxParent.currentInputSource != null && eventData.InputSource.SourceId == BoundingBoxParent.currentInputSource.SourceId)
                {
                    BoundingBoxParent.currentInputSource = null;
                    BoundingBoxParent.currentHandleType = HandleType.None;
                    BoundingBoxParent.currentPointer = null;
                    BoundingBoxParent.grabbedHandle = null;
                    BoundingBoxParent.ResetHandleVisibility();
                    MixedRealityToolkit.InputSystem.PopModalInputHandler();
                }
            }

            void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData) { }
        }

        #region Enums

        /// <summary>
        /// Enum which describes how an object's bounding box is to be flattened.
        /// </summary>
        private enum FlattenModeType
        {
            DoNotFlatten = 0,
            /// <summary>
            /// Flatten the X axis
            /// </summary>
            FlattenX,
            /// <summary>
            /// Flatten the Y axis
            /// </summary>
            FlattenY,
            /// <summary>
            /// Flatten the Z axis
            /// </summary>
            FlattenZ,
            /// <summary>
            /// Flatten the smallest relative axis if it falls below threshold
            /// </summary>
            FlattenAuto,
        }

        /// <summary>
        /// Enum which describes whether a bounding box handle which has been grabbed, is 
        /// a Rotation Handle (sphere) or a Scale Handle( cube)
        /// </summary>
        private enum HandleType
        {
            None = 0,
            Rotation,
            Scale
        }

        /// <summary>
        /// This enum describes which primitive type the wireframe portion of the bounding box
        /// consists of. 
        /// </summary>
        /// <remarks>
        /// Wireframe refers to the thin linkage between the handles. When the handles are invisible
        /// the wireframe looks like an outline box around an object.
        /// </remarks> 
        private enum WireframeType
        {
            Cubic = 0,
            Cylindrical
        }

        [Flags]
        private enum CardinalAxisType
        {
            X = 1,
            Y = 2,
            Z = 3,
        }

        /// <summary>
        /// This enum defines how a particular controller rotates an object when a Rotate handle has been grabbed.
        /// </summary>
        /// <remarks>
        /// a Controller feels more natural when rotation of the controller rotates the object.
        /// the wireframe looks like an outline box around an object.
        /// </remarks> 
        private enum HandleMoveType
        {
            Ray = 0,
            Point
        }

        #endregion Enums

        #region Serialized Fields

        [Header("Debug Options")]

        [SerializeField]
        [Tooltip("Displays the rig in the hierarchy window. Useful for debugging the rig elements.")]
        private bool showRig = false;

        [Header("Bounds Calculation")]

        [SerializeField]
        [Tooltip("For complex objects, automatic bounds calculation may not behave as expected. Use an existing Box Collider (even on a child object) to manually determine bounds of Bounding Box.")]
        private BoxCollider boxColliderToUse = null;

        [Header("Behavior")]

        [SerializeField]
        private bool activateOnStart = false;

        [SerializeField]
        private float scaleMaximum = 2.0f;

        [SerializeField]
        private float scaleMinimum = 0.2f;

        [Header("Wireframe")]

        [SerializeField]
        [FormerlySerializedAs("wireframeOnly")]
        private bool displayHandles = false;

        /// <summary>
        /// Public Property that displays simple wireframe around an object with no scale or rotate handles.
        /// </summary>
        /// <remarks>
        /// this is useful when outlining an object without being able to edit it is desired.
        /// </remarks>
        public bool DisplayHandles
        {
            get => displayHandles;
            set
            {
                if (displayHandles != value)
                {
                    displayHandles = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        private Vector3 wireframePadding = Vector3.zero;

        [SerializeField]
        private FlattenModeType flattenAxis = FlattenModeType.DoNotFlatten;

        [SerializeField]
        private WireframeType wireframeShape = WireframeType.Cubic;

        [SerializeField]
        private Material wireframeMaterial;

        [Header("Handles")]

        [SerializeField]
        [Tooltip("Default materials will be created for Handles and Wireframe if none is specified.")]
        private Material handleMaterial;

        [SerializeField]
        private Material handleGrabbedMaterial;

        [SerializeField]
        private bool showScaleHandles = true;

        /// <summary>
        /// Public property to Set the visibility of the corner cube Scaling handles.
        /// This property can be set independent of the Rotate handles.
        /// </summary>
        public bool ShowScaleHandles
        {
            get => showScaleHandles;
            set
            {
                if (showScaleHandles != value)
                {
                    showScaleHandles = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        private bool showRotateHandles = true;

        /// <summary>
        /// Public property to Set the visibility of the sphere rotating handles.
        /// This property can be set independent of the Scaling handles.
        /// </summary>
        public bool ShowRotateHandles
        {
            get => showRotateHandles;
            set
            {
                if (showRotateHandles != value)
                {
                    showRotateHandles = value;

                    showRotationHandlesPerAxis = (CardinalAxisType)(!showRotateHandles ? 0 : -1);

                    ResetHandleVisibility();
                }
            }
        }

        [EnumFlags]
        [SerializeField]
        [Tooltip("Only show rotation handles for these specific axes.")]
        private CardinalAxisType showRotationHandlesPerAxis = (CardinalAxisType)(-1);

        [SerializeField]
        private float linkRadius = 0.005f;

        [SerializeField]
        private float ballRadius = 0.035f;

        [SerializeField]
        private float cornerRadius = 0.03f;

        #endregion Serialized Fields

        private bool isActive = false;

        /// <summary>
        /// This Public property sets whether the BoundingBox is active (visible)
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (isActive != value)
                {
                    if (value)
                    {
                        CreateRig();
                        rigRoot.gameObject.SetActive(true);
                    }
                    else
                    {
                        DestroyRig();
                    }

                    isActive = value;
                }
            }
        }

        private IMixedRealityPointer currentPointer;
        private IMixedRealityInputSource currentInputSource;

        private Vector3 initialGazePoint = Vector3.zero;
        private Transform rigRoot;
        private BoxCollider cachedTargetCollider;
        private Bounds cachedTargetColliderBounds;
        private HandleMoveType handleMoveType = HandleMoveType.Point;

        private List<Transform> links;
        private List<Transform> corners;
        private List<Transform> balls;
        private List<Renderer> cornerRenderers;
        private List<Renderer> ballRenderers;
        private List<Renderer> linkRenderers;
        private List<Collider> cornerColliders;
        private List<Collider> ballColliders;

        private Ray currentGrabRay;
        private float initialGrabMag;

        private Vector3 initialScale;
        private Vector3 initialGrabPoint;
        private Vector3 currentRotationAxis;
        private Vector3 currentBoundsExtents;
        private Vector3 initialGrabbedPosition;
        private Vector3 currentPosePosition = Vector3.zero;

        private int[] flattenedHandles;

        private GameObject grabbedHandle;

        private bool usingPose = false;

        private HandleType currentHandleType;

        private Vector3[] boundsCorners = new Vector3[8];

        private readonly Vector3[] edgeCenters = new Vector3[12];
        private readonly CardinalAxisType[] edgeAxes = new CardinalAxisType[12];

        private static readonly int numCorners = 8;
        private static readonly int Color = Shader.PropertyToID("_Color");
        private static readonly int InnerGlow = Shader.PropertyToID("_InnerGlow");
        private static readonly int InnerGlowColor = Shader.PropertyToID("_InnerGlowColor");

        #region Monobehaviour Methods

        private void Start()
        {
            if (activateOnStart)
            {
                IsActive = true;
            }
        }

        private void Update()
        {
            if (currentInputSource == null)
            {
                UpdateBounds();
            }
            else
            {
                UpdateBounds();
                TransformRig();
            }

            UpdateRigTransform();
        }

        #endregion Monobehaviour Methods

        private void CreateRig()
        {
            DestroyRig();
            SetMaterials();
            InstantiateRig();

            SetBoundingBoxCollider();

            UpdateBounds();
            CreateLinks();
            CreateScaleHandles();
            CreateRotationHandles();
            UpdateRigTransform();
            Flatten();
            ResetHandleVisibility();
            rigRoot.gameObject.SetActive(false);
        }

        private void DestroyRig()
        {
            if (boxColliderToUse == null)
            {
                Destroy(cachedTargetCollider);
            }
            else
            {
                boxColliderToUse.size -= wireframePadding;
            }

            if (balls != null)
            {
                for (var i = 0; i < balls.Count; i++)
                {
                    Destroy(balls[i]);
                }

                balls.Clear();
            }

            if (links != null)
            {
                for (int i = 0; i < links.Count; i++)
                {
                    Destroy(links[i]);
                }

                links.Clear();
            }

            if (corners != null)
            {
                for (var i = 0; i < corners.Count; i++)
                {
                    Destroy(corners[i]);
                }

                corners.Clear();
            }

            if (rigRoot != null)
            {
                Destroy(rigRoot);
            }
        }

        private void TransformRig()
        {
            if (usingPose)
            {
                TransformHandleWithPoint();
            }
            else
            {
                switch (handleMoveType)
                {
                    case HandleMoveType.Ray:
                        TransformHandleWithRay();
                        break;
                    case HandleMoveType.Point:
                        TransformHandleWithPoint();
                        break;
                    default:
                        Debug.LogWarning($"Unexpected handle move type {handleMoveType}");
                        break;
                }
            }
        }

        private void TransformHandleWithRay()
        {
            if (currentHandleType != HandleType.None)
            {
                currentGrabRay = GetHandleGrabbedRay();
                var grabRayPt = currentGrabRay.origin + (currentGrabRay.direction * initialGrabMag);

                switch (currentHandleType)
                {
                    case HandleType.Rotation:
                        RotateByHandle(grabRayPt);
                        break;
                    case HandleType.Scale:
                        ScaleByHandle(grabRayPt);
                        break;
                    default:
                        Debug.LogWarning($"Unexpected handle type {currentHandleType}");
                        break;
                }
            }
        }

        private void TransformHandleWithPoint()
        {
            if (currentHandleType != HandleType.None)
            {
                Vector3 newGrabbedPosition;

                if (usingPose == false)
                {
                    currentPointer.TryGetPointerPosition(out var newRemotePoint);
                    newGrabbedPosition = initialGrabbedPosition + (newRemotePoint - initialGrabPoint);
                }
                else
                {
                    if (initialGazePoint == Vector3.zero)
                    {
                        return;
                    }

                    newGrabbedPosition = currentPosePosition;
                }

                switch (currentHandleType)
                {
                    case HandleType.Rotation:
                        RotateByHandle(newGrabbedPosition);
                        break;
                    case HandleType.Scale:
                        ScaleByHandle(newGrabbedPosition);
                        break;
                }
            }
        }

        private void RotateByHandle(Vector3 newHandlePosition)
        {
            var rigTransformPosition = rigRoot.transform.position;
            var projPt = Vector3.ProjectOnPlane((newHandlePosition - rigTransformPosition).normalized, currentRotationAxis);
            var rotation = Quaternion.FromToRotation((grabbedHandle.transform.position - rigTransformPosition).normalized, projPt.normalized);
            rotation.ToAngleAxis(out var angle, out var axis);
            transform.RotateAround(rigTransformPosition, axis, angle);
        }

        private void ScaleByHandle(Vector3 newHandlePosition)
        {
            var rigCentroid = rigRoot.transform.position;
            var correctedPt = PointToRay(rigCentroid, grabbedHandle.transform.position, newHandlePosition);
            var startMag = (initialGrabbedPosition - rigCentroid).magnitude;
            var newMag = (correctedPt - rigCentroid).magnitude;

            var ratio = newMag / startMag;
            var newScale = ClampScale(initialScale * ratio, out _);

            //scale from object center
            transform.localScale = newScale;
        }

        private Vector3 GetRotationAxis(GameObject handle)
        {
            for (int i = 0; i < balls.Count; ++i)
            {
                if (handle == balls[i].gameObject)
                {
                    switch (edgeAxes[i])
                    {
                        case CardinalAxisType.X:
                            return rigRoot.transform.up;
                        case CardinalAxisType.Y:
                            return rigRoot.transform.forward;
                        case CardinalAxisType.Z:
                            return rigRoot.transform.right;
                    }
                }
            }

            return Vector3.zero;
        }

        private void CreateLinks()
        {
            edgeAxes[0] = CardinalAxisType.Z;
            edgeAxes[2] = CardinalAxisType.Z;
            edgeAxes[4] = CardinalAxisType.Z;
            edgeAxes[6] = CardinalAxisType.Z;

            edgeAxes[1] = CardinalAxisType.X;
            edgeAxes[3] = CardinalAxisType.X;
            edgeAxes[5] = CardinalAxisType.X;
            edgeAxes[7] = CardinalAxisType.X;

            edgeAxes[08] = CardinalAxisType.Y;
            edgeAxes[09] = CardinalAxisType.Y;
            edgeAxes[10] = CardinalAxisType.Y;
            edgeAxes[11] = CardinalAxisType.Y;

            for (int i = 0; i < edgeCenters.Length; ++i)
            {
                var link = GameObject.CreatePrimitive(wireframeShape == WireframeType.Cubic
                    ? PrimitiveType.Cube
                    : PrimitiveType.Cylinder);

                link.name = $"WireframeEdge_{edgeAxes[i]}_{i}";

                var linkDimensions = GetLinkDimensions();

                switch (edgeAxes[i])
                {
                    case CardinalAxisType.X:
                        link.transform.localScale = new Vector3(linkRadius, linkDimensions.y, linkRadius);
                        link.transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f));
                        break;
                    case CardinalAxisType.Y:
                        link.transform.localScale = new Vector3(linkRadius, linkDimensions.z, linkRadius);
                        link.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
                        break;
                    case CardinalAxisType.Z:
                        link.transform.localScale = new Vector3(linkRadius, linkDimensions.x, linkRadius);
                        link.transform.Rotate(new Vector3(0.0f, 0.0f, 90.0f));
                        break;
                }

                link.transform.position = edgeCenters[i];
                link.transform.parent = rigRoot.transform;

                var linkRenderer = link.GetComponent<Renderer>();
                linkRenderer.shadowCastingMode = ShadowCastingMode.Off;
                linkRenderer.receiveShadows = false;
                linkRenderers.Add(linkRenderer);

                if (wireframeMaterial != null)
                {
                    linkRenderer.material = wireframeMaterial;
                }

                links.Add(link.transform);
            }
        }

        private void CreateScaleHandles()
        {
            var scale = new Vector3(cornerRadius, cornerRadius, cornerRadius);

            for (int i = 0; i < boundsCorners.Length; ++i)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"ScaleHandle_{i}";
                cube.transform.localScale = scale;
                cube.transform.position = boundsCorners[i];
                cube.transform.parent = rigRoot.transform;

                var cubeRenderer = cube.GetComponent<Renderer>();
                cubeRenderer.shadowCastingMode = ShadowCastingMode.Off;
                cubeRenderer.receiveShadows = false;
                cornerRenderers.Add(cubeRenderer);
                cornerColliders.Add(cube.GetComponent<Collider>());
                corners.Add(cube.transform);

                if (handleMaterial != null)
                {
                    cubeRenderer.material = handleMaterial;
                }
            }
        }

        private void CreateRotationHandles()
        {
            var radius = new Vector3(ballRadius, ballRadius, ballRadius);

            for (int i = 0; i < edgeCenters.Length; ++i)
            {
                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = $"RotationHandle_{edgeAxes[i]}_{i}";
                ball.transform.localScale = radius;
                ball.transform.position = edgeCenters[i];
                ball.transform.parent = rigRoot.transform;

                var ballRenderer = ball.GetComponent<Renderer>();
                ballRenderer.shadowCastingMode = ShadowCastingMode.Off;
                ballRenderer.receiveShadows = false;
                ballRenderers.Add(ballRenderer);
                ballColliders.Add(ball.GetComponent<Collider>());
                balls.Add(ball.transform);

                if (handleMaterial != null)
                {
                    ballRenderer.material = handleMaterial;
                }

                if (showRotationHandlesPerAxis == 0 ||
                    showRotationHandlesPerAxis != (CardinalAxisType)(-1) && (showRotationHandlesPerAxis ^ edgeAxes[i]) != 0)
                {
                    ball.SetActive(false);
                }
            }
        }

        private void SetBoundingBoxCollider()
        {
            // Collider.bounds is world space bounding volume.
            // Mesh.bounds is local space bounding volume
            // Renderer.bounds is the same as mesh.bounds but in world space coords

            if (boxColliderToUse != null)
            {
                cachedTargetCollider = boxColliderToUse;
                cachedTargetCollider.transform.hasChanged = true;
            }
            else
            {

                var bounds = transform.GetColliderBounds();
                cachedTargetCollider = gameObject.EnsureComponent<BoxCollider>();
                cachedTargetCollider.center = bounds.center - transform.position;
                cachedTargetCollider.size = bounds.size;
            }

            cachedTargetCollider.size += wireframePadding;
        }

        private void SetMaterials()
        {
            if (wireframeMaterial == null)
            {
                Shader.EnableKeyword("_InnerGlow");
                var shader = Shader.Find("Mixed Reality Toolkit/Standard");

                wireframeMaterial = new Material(shader);
                wireframeMaterial.SetColor(Color, new Color(0.0f, 0.63f, 1.0f));
            }

            if (handleMaterial == null && handleMaterial != wireframeMaterial)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader.EnableKeyword("_InnerGlow");
                var shader = Shader.Find("Mixed Reality Toolkit/Standard");

                handleMaterial = new Material(shader);
                handleMaterial.SetColor(Color, new Color(0.0f, 0.63f, 1.0f));
                handleMaterial.SetFloat(InnerGlow, 1.0f);
                handleMaterial.SetFloatArray(InnerGlowColor, color);
            }

            if (handleGrabbedMaterial == null && handleGrabbedMaterial != handleMaterial && handleGrabbedMaterial != wireframeMaterial)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader.EnableKeyword("_InnerGlow");
                var shader = Shader.Find("Mixed Reality Toolkit/Standard");

                handleGrabbedMaterial = new Material(shader);
                handleGrabbedMaterial.SetColor(Color, new Color(0.0f, 0.63f, 1.0f));
                handleGrabbedMaterial.SetFloat(InnerGlow, 1.0f);
                handleGrabbedMaterial.SetFloatArray(InnerGlowColor, color);
            }
        }

        private void InstantiateRig()
        {
            var rigRootObject = new GameObject($"{gameObject.name}_BB_RigRoot");

            var rig = rigRootObject.AddComponent<BoundingBoxRig>();
            rig.BoundingBoxParent = this;

            if (!showRig)
            {
                rigRootObject.hideFlags = hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            }

            rigRoot = rigRootObject.transform;

            corners = new List<Transform>();
            cornerColliders = new List<Collider>();
            cornerRenderers = new List<Renderer>();
            balls = new List<Transform>();
            ballRenderers = new List<Renderer>();
            ballColliders = new List<Collider>();
            links = new List<Transform>();
            linkRenderers = new List<Renderer>();
        }

        private void CalculateEdgeCenters()
        {
            if (boundsCorners != null && edgeCenters != null)
            {
                edgeCenters[0] = (boundsCorners[0] + boundsCorners[1]) * 0.5f;
                edgeCenters[1] = (boundsCorners[0] + boundsCorners[2]) * 0.5f;
                edgeCenters[2] = (boundsCorners[3] + boundsCorners[2]) * 0.5f;
                edgeCenters[3] = (boundsCorners[3] + boundsCorners[1]) * 0.5f;

                edgeCenters[4] = (boundsCorners[4] + boundsCorners[5]) * 0.5f;
                edgeCenters[5] = (boundsCorners[4] + boundsCorners[6]) * 0.5f;
                edgeCenters[6] = (boundsCorners[7] + boundsCorners[6]) * 0.5f;
                edgeCenters[7] = (boundsCorners[7] + boundsCorners[5]) * 0.5f;

                edgeCenters[8] = (boundsCorners[0] + boundsCorners[4]) * 0.5f;
                edgeCenters[9] = (boundsCorners[1] + boundsCorners[5]) * 0.5f;
                edgeCenters[10] = (boundsCorners[2] + boundsCorners[6]) * 0.5f;
                edgeCenters[11] = (boundsCorners[3] + boundsCorners[7]) * 0.5f;
            }
        }

        private Vector3 ClampScale(Vector3 scale, out bool clamped)
        {
            var finalScale = scale;
            var maximumScale = initialScale * scaleMaximum;
            clamped = false;

            if (scale.x > maximumScale.x || scale.y > maximumScale.y || scale.z > maximumScale.z)
            {
                finalScale = maximumScale;
                clamped = true;
            }

            var minimumScale = initialScale * scaleMinimum;

            if (finalScale.x < minimumScale.x || finalScale.y < minimumScale.y || finalScale.z < minimumScale.z)
            {
                finalScale = minimumScale;
                clamped = true;
            }

            return finalScale;
        }

        private Vector3 GetLinkDimensions()
        {
            var linkLengthAdjustor = wireframeShape == WireframeType.Cubic
                ? 2.0f
                : 1.0f - (6.0f * linkRadius);
            return (currentBoundsExtents * linkLengthAdjustor) + new Vector3(linkRadius, linkRadius, linkRadius);
        }

        private void ResetHandleVisibility()
        {
            bool isVisible;

            //set balls visibility
            if (balls != null)
            {
                isVisible = (!displayHandles && showRotateHandles);

                for (int i = 0; i < ballRenderers.Count; ++i)
                {
                    ballRenderers[i].material = handleMaterial;
                    ballRenderers[i].enabled = isVisible;
                }
            }

            //set corner visibility
            if (corners != null)
            {
                isVisible = (!displayHandles && showScaleHandles);

                for (int i = 0; i < cornerRenderers.Count; ++i)
                {
                    cornerRenderers[i].material = handleMaterial;
                    cornerRenderers[i].enabled = isVisible;
                }
            }

            SetHiddenHandles();
        }

        private void ShowOneHandle(GameObject handle)
        {
            //turn off all balls
            if (balls != null)
            {
                for (int i = 0; i < ballRenderers.Count; ++i)
                {
                    ballRenderers[i].enabled = false;
                }
            }

            //turn off all corners
            if (corners != null)
            {
                for (int i = 0; i < cornerRenderers.Count; ++i)
                {
                    cornerRenderers[i].enabled = false;
                }
            }

            //turn on one handle
            if (handle != null)
            {
                var handleRenderer = handle.GetComponent<Renderer>();
                handleRenderer.material = handleGrabbedMaterial;
                handleRenderer.enabled = true;
            }
        }

        private void UpdateBounds()
        {
            if (cachedTargetCollider == null) { return; }

            // Store current rotation then zero out the rotation so that the bounds
            // are computed when the object is in its 'axis aligned orientation'.
            var currentRotation = transform.rotation;
            transform.rotation = Quaternion.identity;
            Physics.SyncTransforms(); // Update collider bounds
            var boundsExtents = cachedTargetCollider.bounds.extents;
            // After bounds are computed, restore rotation...
            // ReSharper disable once Unity.InefficientPropertyAccess
            transform.rotation = currentRotation;
            Physics.SyncTransforms();

            if (boundsExtents != Vector3.zero)
            {
                if (flattenAxis == FlattenModeType.FlattenAuto)
                {
                    var min = Mathf.Min(boundsExtents.x, Mathf.Min(boundsExtents.y, boundsExtents.z));
                    flattenAxis = min.Equals(boundsExtents.x)
                            ? FlattenModeType.FlattenX
                            : (min.Equals(boundsExtents.y)
                                    ? FlattenModeType.FlattenY
                                    : FlattenModeType.FlattenZ);
                }

                boundsExtents.x = flattenAxis == FlattenModeType.FlattenX ? 0.0f : boundsExtents.x;
                boundsExtents.y = flattenAxis == FlattenModeType.FlattenY ? 0.0f : boundsExtents.y;
                boundsExtents.z = flattenAxis == FlattenModeType.FlattenZ ? 0.0f : boundsExtents.z;
                currentBoundsExtents = boundsExtents;

                GetCornerPositionsFromBounds(new Bounds(Vector3.zero, boundsExtents * 2.0f), ref boundsCorners);
                CalculateEdgeCenters();
            }
        }

        private void UpdateRigTransform()
        {
            if (rigRoot == null) { return; }

            rigRoot.rotation = Quaternion.identity;
            rigRoot.position = Vector3.zero;

            for (int i = 0; i < corners.Count; ++i)
            {
                corners[i].position = boundsCorners[i];
            }

            var linkDimensions = GetLinkDimensions();

            for (int i = 0; i < edgeCenters.Length; ++i)
            {
                balls[i].position = edgeCenters[i];
                links[i].position = edgeCenters[i];

                switch (edgeAxes[i])
                {
                    case CardinalAxisType.X:
                        links[i].localScale = new Vector3(linkRadius, linkDimensions.y, linkRadius);
                        break;
                    case CardinalAxisType.Y:
                        links[i].localScale = new Vector3(linkRadius, linkDimensions.z, linkRadius);
                        break;
                    case CardinalAxisType.Z:
                        links[i].localScale = new Vector3(linkRadius, linkDimensions.x, linkRadius);
                        break;
                }
            }

            // Move rig into position and rotation
            rigRoot.position = transform.TransformPoint(cachedTargetCollider.center);
            rigRoot.rotation = transform.rotation;
        }

        private HandleType GetHandleType(GameObject handle)
        {
            for (int i = 0; i < balls.Count; ++i)
            {
                if (handle == balls[i].gameObject)
                {
                    return HandleType.Rotation;
                }
            }

            for (int i = 0; i < corners.Count; ++i)
            {
                if (handle == corners[i].gameObject)
                {
                    return HandleType.Scale;
                }
            }

            return HandleType.None;
        }

        private Collider GetGrabbedCollider(Ray ray, out float distance)
        {
            float currentDistance;
            float closestDistance = float.MaxValue;
            Collider closestCollider = null;

            for (int i = 0; i < cornerColliders.Count; ++i)
            {
                if (cornerRenderers[i].enabled &&
                    cornerColliders[i].bounds.IntersectRay(ray, out currentDistance) &&
                    currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestCollider = cornerColliders[i];
                }
            }

            for (int i = 0; i < ballColliders.Count; ++i)
            {
                if (ballRenderers[i].enabled &&
                    ballColliders[i].bounds.IntersectRay(ray, out currentDistance) &&
                    currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestCollider = ballColliders[i];
                }
            }

            distance = closestDistance;
            return closestCollider;
        }

        private Ray GetHandleGrabbedRay()
        {
            Ray pointerRay = default;

            if (currentInputSource.Pointers.Length > 0)
            {
                currentInputSource.Pointers[0].TryGetPointingRay(out pointerRay);
            }

            return pointerRay;
        }

        private void Flatten()
        {
            switch (flattenAxis)
            {
                case FlattenModeType.FlattenX:
                    flattenedHandles = new[] { 0, 4, 2, 6 };
                    break;
                case FlattenModeType.FlattenY:
                    flattenedHandles = new[] { 1, 3, 5, 7 };
                    break;
                case FlattenModeType.FlattenZ:
                    flattenedHandles = new[] { 9, 10, 8, 11 };
                    break;
            }

            if (flattenedHandles != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    linkRenderers[flattenedHandles[i]].enabled = false;
                }
            }
        }

        private void SetHiddenHandles()
        {
            if (flattenedHandles != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    ballRenderers[flattenedHandles[i]].enabled = false;
                }
            }
        }

        private static void GetCornerPositionsFromBounds(Bounds bounds, ref Vector3[] positions)
        {
            if (positions == null || positions.Length != numCorners)
            {
                positions = new Vector3[numCorners];
            }

            // Permutate all axes using minCorner and maxCorner.
            var minCorner = bounds.center - bounds.extents;
            var maxCorner = bounds.center + bounds.extents;

            for (int cornerIndex = 0; cornerIndex < numCorners; cornerIndex++)
            {
                positions[cornerIndex] = new Vector3(
                    (cornerIndex & (1 << 0)) == 0 ? minCorner[0] : maxCorner[0],
                    (cornerIndex & (1 << 1)) == 0 ? minCorner[1] : maxCorner[1],
                    (cornerIndex & (1 << 2)) == 0 ? minCorner[2] : maxCorner[2]);
            }
        }

        private static Vector3 PointToRay(Vector3 origin, Vector3 end, Vector3 closestPoint)
        {
            var originToPoint = closestPoint - origin;
            var originToEnd = end - origin;
            var magnitudeAb = originToEnd.sqrMagnitude;
            var dotProduct = Vector3.Dot(originToPoint, originToEnd);
            var distance = dotProduct / magnitudeAb;
            return origin + (originToEnd * distance);
        }
    }
}