// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Interfaces.InputSystem;

namespace XRTK.SDK.Input.Handlers
{
    /// <summary>
    /// Base class for the Mixed Reality Toolkit's SDK input handlers.
    /// </summary>
    public abstract class BaseInputHandler : InputSystemGlobalListener
    {

        private IMixedRealityFocusProvider focusProvider = null;

        protected IMixedRealityFocusProvider FocusProvider
            => focusProvider ?? (focusProvider = InputSystem?.FocusProvider);

        [SerializeField]
        [Tooltip("Is Focus required to receive input events on this GameObject?")]
        private bool isFocusRequired = true;

        /// <summary>
        /// Is Focus required to receive input events on this GameObject?
        /// </summary>
        public virtual bool IsFocusRequired
        {
            get => isFocusRequired;
            protected set => isFocusRequired = value;
        }

        #region MonoBehaviour Implementation

        protected override void OnEnable()
        {
            if (!IsFocusRequired)
            {
                base.OnEnable();
            }
        }

        protected override void Start()
        {
            if (!IsFocusRequired)
            {
                base.Start();
            }
        }

        protected override void OnDisable()
        {
            if (!IsFocusRequired)
            {
                base.OnDisable();
            }
        }

        #endregion MonoBehaviour Implementation
    }
}
