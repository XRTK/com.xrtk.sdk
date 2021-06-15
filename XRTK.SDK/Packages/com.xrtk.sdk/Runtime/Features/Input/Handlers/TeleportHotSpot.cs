// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.LocomotionSystem;
using XRTK.Services;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// SDK component handling teleportation to a specific position and orientation when a user focuses
    /// this <see cref="GameObject"/> and triggers the teleport action.
    /// </summary>
    public class TeleportHotSpot : BaseFocusHandler, ITeleportHotSpot
    {
        private IMixedRealityLocomotionSystem locomotionSystem = null;

        protected IMixedRealityLocomotionSystem LocomotionSystem
            => locomotionSystem ?? (locomotionSystem = MixedRealityToolkit.GetSystem<IMixedRealityLocomotionSystem>());

        #region IMixedRealityFocusHandler Implementation

        /// <inheritdoc />
        public override void OnBeforeFocusChange(FocusEventData eventData)
        {
            base.OnBeforeFocusChange(eventData);

            if (!(eventData.Pointer is ITeleportTargetProvider targetProvider))
            {
                return;
            }

            if (eventData.NewFocusedObject == gameObject)
            {
                eventData.Pointer.TeleportHotSpot = this;

                if (eventData.Pointer.IsInteractionEnabled)
                {
                    LocomotionSystem?.RaiseTeleportCanceled(targetProvider.RequestingLocomotionProvider, eventData.Pointer, this);
                }
            }
            else if (eventData.OldFocusedObject == gameObject)
            {
                eventData.Pointer.TeleportHotSpot = null;

                if (eventData.Pointer.IsInteractionEnabled)
                {
                    LocomotionSystem?.RaiseTeleportCanceled(targetProvider.RequestingLocomotionProvider, eventData.Pointer, this);
                }
            }
        }

        #endregion IMixedRealityFocusHandler Implementation

        #region ITeleportHotSpot Implementation

        /// <inheritdoc />
        public Vector3 Position => transform.position;

        /// <inheritdoc />
        public Vector3 Normal => transform.up;

        /// <inheritdoc />
        public bool IsActive => isActiveAndEnabled;

        [SerializeField]
        [Tooltip("Should the destination orientation be overridden? " +
                 "Useful when you want to orient the user in a specific direction when they teleport to this position. " +
                 "Override orientation is the transform forward of the GameObject this component is attached to.")]
        private bool overrideOrientation = false;

        /// <inheritdoc />
        public bool OverrideTargetOrientation => overrideOrientation;

        /// <inheritdoc />
        public float TargetOrientation => transform.eulerAngles.y;

        /// <inheritdoc />
        public GameObject GameObjectReference => gameObject;

        #endregion ITeleportHotSpot Implementation

        private void OnDrawGizmos()
        {
            Gizmos.color = IsActive ? Color.green : Color.red;
            Gizmos.DrawLine(Position + (Vector3.up * 0.1f), Position + (Vector3.up * 0.1f) + (transform.forward * 0.1f));
            Gizmos.DrawSphere(Position + (Vector3.up * 0.1f) + (transform.forward * 0.1f), 0.01f);
        }
    }
}
