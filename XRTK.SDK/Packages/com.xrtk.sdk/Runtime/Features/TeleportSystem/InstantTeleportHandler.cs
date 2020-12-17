// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Teleport;
using XRTK.Extensions;
using XRTK.Interfaces.TeleportSystem;

namespace XRTK.SDK.TeleportSystem
{
    /// <summary>
    /// This <see cref="IMixedRealityTeleportSystem"/> handler implementation will
    /// perform an instant teleport to the target location.
    /// </summary>
    [System.Runtime.InteropServices.Guid("cb3ee7a8-114e-44d6-9edc-cd55049fefb6")]
    public class InstantTeleportHandler : BaseTeleportHandler
    {
        [SerializeField]
        [Tooltip("Assign the transform with the camera component attached. If not set, the component uses its own transform.")]
        private Transform cameraTransform = null;

        [SerializeField]
        [Tooltip("Assign the transform being teleported to the target location. If not set, the component game object's parent transform is used.")]
        private Transform teleportTransform = null;

        private Vector3 targetPosition;
        private Vector3 targetRotation;

        /// <summary>
        /// Awake is called when the instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (cameraTransform.IsNull())
            {
                cameraTransform = transform;
            }

            if (teleportTransform.IsNull())
            {
                teleportTransform = cameraTransform.parent;
                Debug.Assert(teleportTransform != null,
                    $"{nameof(InstantTeleportHandler)} requires that the camera be parented under another object " +
                    $"or a parent transform was assigned in editor.");
            }
        }

        /// <inheritdoc />
        public override void OnTeleportStarted(TeleportEventData eventData)
        {
            if (eventData.used)
            {
                return;
            }

            eventData.Use();

            targetRotation = Vector3.zero;
            targetPosition = eventData.Pointer.Result.EndPoint;
            targetRotation.y = eventData.Pointer.PointerOrientation;

            if (eventData.HotSpot != null)
            {
                targetPosition = eventData.HotSpot.Position;
                if (eventData.HotSpot.OverrideTargetOrientation)
                {
                    targetRotation.y = eventData.HotSpot.TargetOrientation;
                }
            }

            var height = targetPosition.y;
            targetPosition -= cameraTransform.position - teleportTransform.position;
            targetPosition.y = height;
            teleportTransform.position = targetPosition;
            teleportTransform.RotateAround(cameraTransform.position, Vector3.up, targetRotation.y - cameraTransform.eulerAngles.y);

            TeleportSystem.RaiseTeleportComplete(eventData.Pointer, eventData.HotSpot);
        }
    }
}
