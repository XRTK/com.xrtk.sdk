// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using XRTK.Definitions.Controllers.Hands;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Interfaces.Providers.Controllers.Hands;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Base hand controller visualizer implementation.
    /// </summary>
    [System.Runtime.InteropServices.Guid("5d844e0b-f913-46b8-bc3b-fa6429e62c60")]
    public class DefaultHandControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer
    {
        private readonly Dictionary<TrackedHandJoint, Transform> jointTransforms = new Dictionary<TrackedHandJoint, Transform>();
        private BoxCollider handBoundsModeCollider;
        private Dictionary<TrackedHandJoint, CapsuleCollider> fingerBoundsModeColliders = new Dictionary<TrackedHandJoint, CapsuleCollider>();
        private const float fingerColliderRadius = .007f;
        private const int capsuleColliderZAxis = 2;
        HandControllerJointsVisualizer jointsVisualizer;
        private HandControllerMeshVisualizer meshVisualizer;

        [SerializeField]
        [Tooltip("Visualization prefab instantiated once joint rendering mode is enabled for the first time.")]
        private GameObject jointsModePrefab = null;

        [SerializeField]
        [Tooltip("Visualization prefab instantiated once mesh rendering mode is enabled for the first time.")]
        private GameObject meshModePrefab = null;

        /// <inheritdoc />
        public GameObject GameObject
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
        /// If using physics with hand, the actual hand visualization is done
        /// on a companion game object which is connected to the <see cref="GameObject"/>
        /// using a <see cref="FixedJoint"/>. For physics to work properly while maintaining
        /// the platforms controller tracking we cannot attach colliders and a rigidbody to the
        /// <see cref="GameObject"/> since that would cause crazy behaviour on controller movement.
        /// </summary>
        private GameObject PhysicsCompanionGameObject { get; set; }

        /// <summary>
        /// The actual game object that is parent to all controller visualization of this hand controller.
        /// </summary>
        public GameObject HandVisualizationGameObject => HandControllerDataProvider.HandPhysicsEnabled ? PhysicsCompanionGameObject : GameObject;

        private IMixedRealityHandControllerDataProvider handControllerDataProvider;

        /// <summary>
        /// The active hand controller data provider.
        /// </summary>
        protected IMixedRealityHandControllerDataProvider HandControllerDataProvider => handControllerDataProvider ?? (handControllerDataProvider = (IMixedRealityHandControllerDataProvider)Controller.ControllerDataProvider);

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            // In case physics are enabled we need to take destroy the
            // physics game object as well when destroying the hand visualizer.
            if (PhysicsCompanionGameObject != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(PhysicsCompanionGameObject);
                }
                else
                {
                    Destroy(PhysicsCompanionGameObject);
                }
            }

            base.OnDestroy();
        }

        /// <inheritdoc />
        public override void OnInputChanged(InputEventData<HandData> eventData)
        {
            if (eventData.Handedness != Controller.ControllerHandedness)
            {
                return;
            }

            // Update the visualizers tracking state.
            TrackingState = eventData.InputData.IsTracked
                ? Definitions.Devices.TrackingState.Tracked
                : Definitions.Devices.TrackingState.NotTracked;

            // It's important to update physics
            // configuration before updating joints.
            UpdatePhysicsConfiguration();

            var handData = eventData.InputData;
            UpdateHandJointTransforms(handData);

            // With joints updated, we can update colliders.
            UpdateHandColliders();

            // Update visualizers depending on the current mode.
            UpdateRendering(handData);
        }

        private void UpdateHandJointTransforms(HandData handData)
        {
            var jointPoses = handData.Joints.ToJointPoseDictionary();

            foreach (var handJoint in jointPoses.Keys)
            {
                var jointTransform = GetOrCreateJointTransform(handJoint);
                var jointPose = jointPoses[handJoint];
                jointTransform.localPosition = jointPose.Position;
                jointTransform.rotation = jointPose.Rotation;
            }
        }

        #region Hand Colliders / Physics

        private void UpdatePhysicsConfiguration()
        {
            if (HandControllerDataProvider.HandPhysicsEnabled)
            {
                // If we are using hand physics, we need to make sure
                // the physics companion is setup properly.
                if (PhysicsCompanionGameObject != null)
                {
                    PhysicsCompanionGameObject.SetActive(true);
                    return;
                }

                PhysicsCompanionGameObject = new GameObject($"{GameObject.name}_Physics");
                PhysicsCompanionGameObject.transform.parent = GameObject.transform.parent;
                var parentConstraint = PhysicsCompanionGameObject.AddComponent<ParentConstraint>();
                parentConstraint.AddSource(new ConstraintSource
                {
                    sourceTransform = GameObject.transform
                });

                // Setup the kinematic rigidbody on the actual controller game object.
                Rigidbody controllerRigidbody = GameObject.GetOrAddComponent<Rigidbody>();
                controllerRigidbody.mass = .46f; // 0.46 Kg average human hand weight
                controllerRigidbody.isKinematic = true;
                controllerRigidbody.useGravity = false;
                controllerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                // Make the physics proxy a fixed joint rigidbody to the controller
                // and give it an adamantium coated connection so it doesn't break.
                Rigidbody physicsRigidbody = PhysicsCompanionGameObject.GetOrAddComponent<Rigidbody>();
                physicsRigidbody.mass = .46f; // 0.46 Kg average human hand weight
                physicsRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                FixedJoint fixedJoint = PhysicsCompanionGameObject.GetOrAddComponent<FixedJoint>();
                fixedJoint.connectedBody = controllerRigidbody;
                fixedJoint.breakForce = float.MaxValue;
                fixedJoint.breakTorque = float.MaxValue;
            }
            else if (PhysicsCompanionGameObject != null)
            {
                PhysicsCompanionGameObject.SetActive(false);
            }
        }

        private void UpdateHandColliders()
        {
            if (HandControllerDataProvider.HandPhysicsEnabled)
            {
                var handController = (IMixedRealityHandController)Controller;

                if (HandControllerDataProvider.BoundsMode == HandBoundsMode.Fingers)
                {
                    // Make sure to disable other colliders not needed for the fingers mode.
                    DisableHandBounds();

                    if (handController.TryGetBounds(TrackedHandBounds.Thumb, out Bounds[] thumbBounds))
                    {
                        // Thumb bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = thumbBounds[0];
                        Bounds middleToTip = thumbBounds[1];

                        var thumbKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.ThumbMetacarpalJoint).gameObject;
                        var capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.ThumbMetacarpalJoint, thumbKnuckleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, thumbKnuckleGameObject.transform);

                        var thumbMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.ThumbProximalJoint).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.ThumbProximalJoint, thumbMiddleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, thumbMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.IndexFinger, out Bounds[] indexFingerBounds))
                    {
                        // Index finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = indexFingerBounds[0];
                        Bounds middleToTip = indexFingerBounds[1];

                        var indexKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.IndexKnuckle).gameObject;
                        var capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.IndexKnuckle, indexKnuckleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, indexKnuckleGameObject.transform);

                        var indexMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.IndexMiddleJoint).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.IndexMiddleJoint, indexMiddleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, indexMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.MiddleFinger, out Bounds[] middleFingerBounds))
                    {
                        // Middle finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = middleFingerBounds[0];
                        Bounds middleToTip = middleFingerBounds[1];

                        var middleKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.MiddleKnuckle).gameObject;
                        var capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.MiddleKnuckle, middleKnuckleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, middleKnuckleGameObject.transform);

                        var middleMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.MiddleMiddleJoint).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.MiddleMiddleJoint, middleMiddleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, middleMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.RingFinger, out Bounds[] ringFingerBounds))
                    {
                        // Ring finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = ringFingerBounds[0];
                        Bounds middleToTip = ringFingerBounds[1];

                        var ringKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.RingKnuckle).gameObject;
                        var capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.RingKnuckle, ringKnuckleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, ringKnuckleGameObject.transform);

                        var ringMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.RingMiddleJoint).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.RingMiddleJoint, ringMiddleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, ringMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.Pinky, out Bounds[] pinkyFingerBounds))
                    {
                        // Pinky finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = pinkyFingerBounds[0];
                        Bounds middleToTip = pinkyFingerBounds[1];

                        var pinkyKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.PinkyKnuckle).gameObject;
                        var capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.PinkyKnuckle, pinkyKnuckleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, pinkyKnuckleGameObject.transform);

                        var pinkyMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.PinkyMiddleJoint).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.PinkyMiddleJoint, pinkyMiddleGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, pinkyMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.Palm, out Bounds[] palmBounds))
                    {
                        // For the palm we create a composite collider using a capsule collider per
                        // finger for the area metacarpal <-> knuckle.
                        Bounds indexPalmBounds = palmBounds[0];
                        var indexMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.IndexMetacarpal).gameObject;
                        var capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.IndexMetacarpal, indexMetacarpalGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, indexPalmBounds, indexMetacarpalGameObject.transform);

                        Bounds middlePalmBounds = palmBounds[1];
                        var middleMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.MiddleMetacarpal).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.MiddleMetacarpal, middleMetacarpalGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, middlePalmBounds, middleMetacarpalGameObject.transform);

                        Bounds ringPalmBounds = palmBounds[2];
                        var ringMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.RingMetacarpal).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.RingMetacarpal, ringMetacarpalGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, ringPalmBounds, ringMetacarpalGameObject.transform);

                        Bounds pinkyPalmBounds = palmBounds[3];
                        var pinkyMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.PinkyMetacarpal).gameObject;
                        capsuleCollider = GetOrCreateCapsuleCollider(TrackedHandJoint.PinkyMetacarpal, pinkyMetacarpalGameObject);
                        ConfigureCapsuleCollider(capsuleCollider, pinkyPalmBounds, pinkyMetacarpalGameObject.transform);
                    }
                }
                else if (HandControllerDataProvider.BoundsMode == HandBoundsMode.Hand)
                {
                    DisableFingerBounds();

                    if (handController.TryGetBounds(TrackedHandBounds.Hand, out Bounds[] handBounds))
                    {
                        // For full hand bounds we'll only get one bounds entry, which is a box
                        // encapsulating the whole hand.
                        Bounds fullHandBounds = handBounds[0];
                        handBoundsModeCollider = HandVisualizationGameObject.GetOrAddComponent<BoxCollider>();
                        handBoundsModeCollider.enabled = true;
                        handBoundsModeCollider.center = fullHandBounds.center;
                        handBoundsModeCollider.size = fullHandBounds.size;
                        handBoundsModeCollider.isTrigger = HandControllerDataProvider.UseTriggers;
                    }
                }
            }
        }

        private void ConfigureCapsuleCollider(CapsuleCollider collider, Bounds bounds, Transform jointTransform)
        {
            collider.radius = fingerColliderRadius;
            collider.direction = capsuleColliderZAxis;
            collider.height = bounds.size.magnitude;
            collider.center = jointTransform.InverseTransformPoint(bounds.center);
            collider.isTrigger = HandControllerDataProvider.UseTriggers;
            collider.enabled = true;
        }

        private CapsuleCollider GetOrCreateCapsuleCollider(TrackedHandJoint trackedHandJoint, GameObject forObject)
        {
            CapsuleCollider collider;
            if (fingerBoundsModeColliders.ContainsKey(trackedHandJoint))
            {
                collider = fingerBoundsModeColliders[trackedHandJoint];
            }
            else
            {
                collider = forObject.GetOrAddComponent<CapsuleCollider>();
                fingerBoundsModeColliders.Add(trackedHandJoint, collider);
            }

            return collider;
        }

        private void DisableFingerBounds()
        {
            foreach (var item in fingerBoundsModeColliders)
            {
                item.Value.enabled = false;
            }
        }

        private void DisableHandBounds()
        {
            if (handBoundsModeCollider != null)
            {
                handBoundsModeCollider.enabled = false;
            }
        }

        #endregion

        /// <summary>
        /// Gets the proxy transform for a given tracked hand joint or creates
        /// it if it does not exist yet.
        /// </summary>
        /// <param name="handJoint">The hand joint a transform should be returned for.</param>
        /// <returns>Joint transform.</returns>
        public Transform GetOrCreateJointTransform(TrackedHandJoint handJoint)
        {
            if (jointTransforms.TryGetValue(handJoint, out Transform existingJointTransform))
            {
                existingJointTransform.parent = HandVisualizationGameObject.transform;
                existingJointTransform.gameObject.SetActive(true);
                return existingJointTransform;
            }

            Transform jointTransform = new GameObject($"{handJoint}ProxyTransform").transform;
            jointTransform.parent = HandVisualizationGameObject.transform;
            jointTransforms.Add(handJoint, jointTransform.transform);

            return jointTransform;
        }

        private void UpdateRendering(HandData handData)
        {
            var renderingMode = HandControllerDataProvider.RenderingMode;
            if (renderingMode != HandRenderingMode.None)
            {
                // Fallback to joints rendering if the platform did not provide
                // any mesh data.
                if (renderingMode == HandRenderingMode.Mesh &&
                    handData.Mesh.Empty)
                {
                    renderingMode = HandRenderingMode.Joints;
                }

                if (renderingMode == HandRenderingMode.Joints)
                {
                    if (meshVisualizer != null)
                    {
                        meshVisualizer.gameObject.SetActive(false);
                    }

                    if (jointsVisualizer == null)
                    {
                        jointsVisualizer = Instantiate(jointsModePrefab, HandVisualizationGameObject.transform).GetComponent<HandControllerJointsVisualizer>();
                    }

                    jointsVisualizer.gameObject.SetActive(true);
                    jointsVisualizer.UpdateVisualization(this);
                }
                else if (renderingMode == HandRenderingMode.Mesh)
                {
                    if (jointsVisualizer != null)
                    {
                        jointsVisualizer.gameObject.SetActive(false);
                    }

                    if (meshVisualizer == null)
                    {
                        meshVisualizer = Instantiate(meshModePrefab, HandVisualizationGameObject.transform).GetComponent<HandControllerMeshVisualizer>();
                    }

                    meshVisualizer.gameObject.SetActive(true);
                    meshVisualizer.UpdateVisualization(handData);
                }
            }
            else
            {
                if (jointsVisualizer != null)
                {
                    jointsVisualizer.gameObject.SetActive(false);
                }

                if (meshVisualizer != null)
                {
                    meshVisualizer.gameObject.SetActive(false);
                }
            }
        }
    }
}