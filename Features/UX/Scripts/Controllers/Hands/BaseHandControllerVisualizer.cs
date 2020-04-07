// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers.Hands;
using XRTK.EventDatum.Input;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem.Controllers.Hands;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.SDK.Input.Handlers;

namespace XRTK.SDK.UX.Controllers.Hands
{
    /// <summary>
    /// Base hand controller visualizer implementation.
    /// </summary>
    public class BaseHandControllerVisualizer : ControllerPoseSynchronizer, IMixedRealityControllerVisualizer
    {
        private readonly Dictionary<TrackedHandJoint, Transform> jointTransforms = new Dictionary<TrackedHandJoint, Transform>();
        private const float fingerColliderRadius = .007f;
        private const int capsuleColliderZAxis = 2;

        /// <inheritdoc />
        public GameObject GameObjectProxy
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
        /// on a companion game object which is connected to the <see cref="GameObjectProxy"/>
        /// using a <see cref="FixedJoint"/>. For physics to work properly while maintaining
        /// the platforms controller tracking we cannot attach colliders and a rigidbody to the
        /// <see cref="GameObjectProxy"/> since that would cause crazy behaviour on controller movement.
        /// </summary>
        private GameObject PhysicsCompanionGameObject { get; set; }

        /// <summary>
        /// The actual game object that is parent to all controller visualization of this hand controller.
        /// </summary>
        protected GameObject HandVisualizationGameObject => ((IMixedRealityHandControllerDataProvider)Controller.ControllerDataProvider).HandPhysicsEnabled ? PhysicsCompanionGameObject : GameObjectProxy;

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
            if (GameObjectProxy != HandVisualizationGameObject)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(HandVisualizationGameObject);
                }
                else
                {
                    Destroy(HandVisualizationGameObject);
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
        }

        private void UpdateHandJointTransforms(HandData handData)
        {
            var jointPoses = handData.Joints.ToJointPoseDictionary();

            foreach (var handJoint in jointPoses.Keys)
            {
                if (handJoint != TrackedHandJoint.None)
                {
                    var jointTransform = GetOrCreateJointTransform(handJoint);
                    var jointPose = jointPoses[handJoint];
                    jointTransform.position = jointPose.Position;
                    jointTransform.rotation = jointPose.Rotation;
                }
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
                    return;
                }

                PhysicsCompanionGameObject = new GameObject($"{GameObjectProxy.name}_Physics");
                PhysicsCompanionGameObject.transform.parent = GameObjectProxy.transform.parent;

                // Setup the kinematic rigidbody on the actual controller game object.
                Rigidbody controllerRigidbody = GameObjectProxy.GetOrAddComponent<Rigidbody>();
                controllerRigidbody.isKinematic = true;
                controllerRigidbody.useGravity = false;

                // Make the physics proxy a fixed joint rigidbody to the controller
                // and give it an adamantium coated connection so it doesn't break.
                Rigidbody physicsRigidbody = PhysicsCompanionGameObject.GetOrAddComponent<Rigidbody>();
                physicsRigidbody.mass = float.MaxValue;
                FixedJoint fixedJoint = PhysicsCompanionGameObject.GetOrAddComponent<FixedJoint>();
                fixedJoint.connectedBody = controllerRigidbody;
                fixedJoint.breakForce = float.MaxValue;
                fixedJoint.breakTorque = float.MaxValue;
            }
        }

        private void UpdateHandColliders()
        {
            if (HandControllerDataProvider.HandPhysicsEnabled)
            {
                var handController = (IMixedRealityHandController)Controller;

                if (HandControllerDataProvider.BoundsMode == HandBoundsMode.Fingers)
                {
                    if (handController.TryGetBounds(TrackedHandBounds.Thumb, out Bounds[] thumbBounds))
                    {
                        // Thumb bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = thumbBounds[0];
                        Bounds middleToTip = thumbBounds[1];

                        GameObject thumbKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.ThumbMetacarpalJoint).gameObject;
                        CapsuleCollider capsuleCollider = thumbKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, thumbKnuckleGameObject.transform);

                        GameObject thumbMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.ThumbProximalJoint).gameObject;
                        capsuleCollider = thumbMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, thumbMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.IndexFinger, out Bounds[] indexFingerBounds))
                    {
                        // Index finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = indexFingerBounds[0];
                        Bounds middleToTip = indexFingerBounds[1];

                        GameObject indexKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.IndexKnuckle).gameObject;
                        CapsuleCollider capsuleCollider = indexKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, indexKnuckleGameObject.transform);

                        GameObject indexMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.IndexMiddleJoint).gameObject;
                        capsuleCollider = indexMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, indexMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.MiddleFinger, out Bounds[] middleFingerBounds))
                    {
                        // Middle finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = middleFingerBounds[0];
                        Bounds middleToTip = middleFingerBounds[1];

                        GameObject middleKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.MiddleKnuckle).gameObject;
                        CapsuleCollider capsuleCollider = middleKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, middleKnuckleGameObject.transform);

                        GameObject middleMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.MiddleMiddleJoint).gameObject;
                        capsuleCollider = middleMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, middleMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.RingFinger, out Bounds[] ringFingerBounds))
                    {
                        // Ring finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = ringFingerBounds[0];
                        Bounds middleToTip = ringFingerBounds[1];

                        GameObject ringKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.RingKnuckle).gameObject;
                        CapsuleCollider capsuleCollider = ringKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, ringKnuckleGameObject.transform);

                        GameObject ringMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.RingMiddleJoint).gameObject;
                        capsuleCollider = ringMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, ringMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.Pinky, out Bounds[] pinkyFingerBounds))
                    {
                        // Pinky finger bounds are made up of two capsule collider bounds entries.
                        Bounds knuckleToMiddle = pinkyFingerBounds[0];
                        Bounds middleToTip = pinkyFingerBounds[1];

                        GameObject pinkyKnuckleGameObject = GetOrCreateJointTransform(TrackedHandJoint.PinkyKnuckle).gameObject;
                        CapsuleCollider capsuleCollider = pinkyKnuckleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, knuckleToMiddle, pinkyKnuckleGameObject.transform);

                        GameObject pinkyMiddleGameObject = GetOrCreateJointTransform(TrackedHandJoint.PinkyMiddleJoint).gameObject;
                        capsuleCollider = pinkyMiddleGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, middleToTip, pinkyMiddleGameObject.transform);
                    }

                    if (handController.TryGetBounds(TrackedHandBounds.Palm, out Bounds[] palmBounds))
                    {
                        // For the palm we create a composite collider using a capsule collider per
                        // finger for the area metacarpal <-> knuckle.
                        Bounds indexPalmBounds = palmBounds[0];
                        GameObject indexMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.IndexMetacarpal).gameObject;
                        CapsuleCollider capsuleCollider = indexMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, indexPalmBounds, indexMetacarpalGameObject.transform);

                        Bounds middlePalmBounds = palmBounds[1];
                        GameObject middleMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.MiddleMetacarpal).gameObject;
                        capsuleCollider = middleMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, middlePalmBounds, middleMetacarpalGameObject.transform);

                        Bounds ringPalmBounds = palmBounds[2];
                        GameObject ringMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.RingMetacarpal).gameObject;
                        capsuleCollider = ringMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, ringPalmBounds, ringMetacarpalGameObject.transform);

                        Bounds pinkyPalmBounds = palmBounds[3];
                        GameObject pinkyMetacarpalGameObject = GetOrCreateJointTransform(TrackedHandJoint.PinkyMetacarpal).gameObject;
                        capsuleCollider = pinkyMetacarpalGameObject.GetOrAddComponent<CapsuleCollider>();
                        ConfigureCapsuleCollider(capsuleCollider, pinkyPalmBounds, pinkyMetacarpalGameObject.transform);
                    }
                }
                else if (HandControllerDataProvider.BoundsMode == HandBoundsMode.Hand)
                {
                    if (handController.TryGetBounds(TrackedHandBounds.Hand, out Bounds[] handBounds))
                    {
                        // For full hand bounds we'll only get one bounds entry, which is a box
                        // encapsulating the whole hand.
                        Bounds fullHandBounds = handBounds[0];
                        BoxCollider boxCollider = HandVisualizationGameObject.GetOrAddComponent<BoxCollider>();
                        boxCollider.center = fullHandBounds.center;
                        boxCollider.size = fullHandBounds.size;
                        boxCollider.isTrigger = HandControllerDataProvider.UseTriggers;
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
        }

        #endregion

        /// <summary>
        /// Gets the proxy transform for a given tracked hand joint or creates
        /// it if it does not exist yet.
        /// </summary>
        /// <param name="handJoint">The hand joint a transform should be returned for.</param>
        /// <returns>Joint transform.</returns>
        protected Transform GetOrCreateJointTransform(TrackedHandJoint handJoint)
        {
            if (jointTransforms.TryGetValue(handJoint, out Transform existingJointTransform))
            {
                existingJointTransform.gameObject.SetActive(true);
                return existingJointTransform;
            }

            Transform jointTransform = new GameObject($"{handJoint}ProxyTransform").transform;
            jointTransform.parent = HandVisualizationGameObject.transform;
            jointTransforms.Add(handJoint, jointTransform.transform);

            return jointTransform;
        }
    }
}