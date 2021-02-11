// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.EventDatum.Input;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.InputSystem.Handlers;
using XRTK.Services;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// Base Component for handling Focus on <see cref="GameObject"/>s.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public abstract class BaseFocusHandler : MonoBehaviour,
        IMixedRealityFocusHandler,
        IMixedRealityFocusChangedHandler
    {
        [SerializeField]
        [Tooltip("Is focus enabled for this component?")]
        private bool focusEnabled = true;

        /// <summary>
        /// Is focus enabled for this <see cref="GameObject"/>?
        /// </summary>
        public virtual bool FocusEnabled
        {
            get => focusEnabled;
            set => focusEnabled = value;
        }

        private IMixedRealityInputSystem inputSystem = null;

        protected IMixedRealityInputSystem InputSystem
            => inputSystem ?? (inputSystem = MixedRealityToolkit.GetSystem<IMixedRealityInputSystem>());

        private IMixedRealityFocusProvider focusProvider = null;

        protected IMixedRealityFocusProvider FocusProvider
            => focusProvider ?? (focusProvider = InputSystem?.FocusProvider);

        /// <summary>
        /// Does this object currently have focus by any <see cref="IMixedRealityPointer"/>?
        /// </summary>
        public virtual bool HasFocus => FocusEnabled && ActivePointers.Count > 0;

        /// <summary>
        /// The list of <see cref="IMixedRealityPointer"/>s that are currently focused on this <see cref="GameObject"/>
        /// </summary>
        public IReadOnlyList<IMixedRealityPointer> ActivePointers => activePointers;

        private readonly List<IMixedRealityPointer> activePointers = new List<IMixedRealityPointer>(0);

        /// <inheritdoc />
        public virtual void OnFocusEnter(FocusEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnFocusExit(FocusEventData eventData) { }

        /// <inheritdoc />
        public virtual void OnBeforeFocusChange(FocusEventData eventData)
        {
            // If we're the new target object,
            // add the pointer to the list of focusers.
            if (eventData.NewFocusedObject == gameObject)
            {
                eventData.Pointer.FocusHandler = this;
                activePointers.Add(eventData.Pointer);
            }
            // If we're the old focused target object,
            // remove the pointer from our list.
            else if (eventData.OldFocusedObject == gameObject)
            {
                activePointers.Remove(eventData.Pointer);

                // If there is no new focused target
                // clear the FocusTarget field from the Pointer.
                if (eventData.NewFocusedObject == null)
                {
                    eventData.Pointer.FocusHandler = null;
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnFocusChanged(FocusEventData eventData) { }
    }
}
