// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Utilities;

namespace XRTK.SDK.UX.Collections
{
    [DisallowMultipleComponent]
    public abstract class BaseObjectCollection : MonoBehaviour
    {
        /// <summary>
        /// Action called when collection is updated
        /// </summary>
        public Action<BaseObjectCollection> OnCollectionUpdated { get; set; }

        protected readonly List<ObjectCollectionNode> nodeList = new List<ObjectCollectionNode>();

        [SerializeField]
        [Tooltip("Whether to include space for inactive transforms in the layout")]
        private bool ignoreInactiveTransforms = true;

        /// <summary>
        /// Whether to include space for inactive transforms in the layout
        /// </summary>
        public bool IgnoreInactiveTransforms
        {
            get => ignoreInactiveTransforms;
            set => ignoreInactiveTransforms = value;
        }

        [SerializeField]
        [Tooltip("Type of sorting to use")]
        private CollationOrderType sortType = CollationOrderType.None;

        /// <summary>
        /// Type of sorting to use.
        /// </summary>
        public CollationOrderType SortType
        {
            get => sortType;
            set => sortType = value;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.hierarchyChanged += () =>
            {
                if (transform.hasChanged)
                {
                    UpdateCollection();
                }
            };
#endif
            UpdateCollection();
        }

        /// <summary>
        /// Gets the current list of <see cref="ObjectCollectionNode"/>s
        /// </summary>
        public virtual List<ObjectCollectionNode> GetCollection()
        {
            // Check for empty nodes and remove them
            var emptyNodes = new List<ObjectCollectionNode>();

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (nodeList[i].Transform == null ||
                    nodeList[i].Transform.parent == null ||
                    !(nodeList[i].Transform.parent.gameObject == gameObject) ||
                    !nodeList[i].Transform.gameObject.activeSelf && IgnoreInactiveTransforms)
                {
                    emptyNodes.Add(nodeList[i]);
                }
            }

            // Now delete the empty nodes
            for (int i = 0; i < emptyNodes.Count; i++)
            {
                nodeList.Remove(emptyNodes[i]);
            }

            emptyNodes.Clear();

            // Check when children change and adjust
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);

#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(child, "ObjectCollection modify transform");
#endif

                if (!ContainsNode(child) && (child.gameObject.activeSelf || !IgnoreInactiveTransforms))
                {
                    nodeList.Add(new ObjectCollectionNode { Name = child.name, Transform = child });
                }
            }

            return nodeList;
        }

        /// <summary>
        /// Rebuilds / updates the collection layout.
        /// Update collection is called from the editor button on the inspector.
        /// </summary>
        public virtual void UpdateCollection()
        {
            GetCollection();

            switch (SortType)
            {
                case CollationOrderType.ChildOrder:
                    nodeList.Sort((c1, c2) => (c1.Transform.GetSiblingIndex().CompareTo(c2.Transform.GetSiblingIndex())));
                    break;

                case CollationOrderType.Alphabetical:
                    nodeList.Sort((c1, c2) => (string.CompareOrdinal(c1.Name, c2.Name)));
                    break;

                case CollationOrderType.AlphabeticalReversed:
                    nodeList.Sort((c1, c2) => (string.CompareOrdinal(c1.Name, c2.Name)));
                    nodeList.Reverse();
                    break;

                case CollationOrderType.ChildOrderReversed:
                    nodeList.Sort((c1, c2) => (c1.Transform.GetSiblingIndex().CompareTo(c2.Transform.GetSiblingIndex())));
                    nodeList.Reverse();
                    break;
            }

            LayoutChildren();

            OnCollectionUpdated?.Invoke(this);
        }

        /// <summary>
        /// Check if a node exists in the NodeList.
        /// </summary>
        protected bool ContainsNode(Transform node)
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                if (nodeList[i].Transform == node)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Implement for laying out all children when UpdateCollection is called.
        /// </summary>
        protected abstract void LayoutChildren();
    }
}