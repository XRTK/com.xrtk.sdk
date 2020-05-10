// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Controllers.Hands;

namespace XRTK.SDK.UX.Controllers.Hands
{
    public class HandControllerJointsVisualizer : MonoBehaviour
    {
        private readonly Dictionary<TrackedHandJoint, GameObject> jointVisualizations = new Dictionary<TrackedHandJoint, GameObject>();
        private DefaultHandControllerVisualizer mainVisualizer;

        [SerializeField]
        [Tooltip("The wrist prefab to use.")]
        private GameObject wristPrefab = null;

        [SerializeField]
        [Tooltip("The joint prefab to use.")]
        private GameObject jointPrefab = null;

        [SerializeField]
        [Tooltip("The joint prefab to use for palm.")]
        private GameObject palmPrefab = null;

        [SerializeField]
        [Tooltip("The joint prefab to use for the index tip (point of interaction.")]
        private GameObject fingertipPrefab = null;

        [SerializeField]
        [Tooltip("Material tint color for index fingertip.")]
        private Color indexFingertipColor = Color.cyan;

        private void OnDisable()
        {
            foreach (var jointVisualization in jointVisualizations)
            {
                jointVisualization.Value.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the joints visuailzation.
        /// </summary>
        /// <param name="mainVisualizer">The managing visuailzer component.</param>
        public void UpdateVisualization(DefaultHandControllerVisualizer mainVisualizer)
        {
            this.mainVisualizer = mainVisualizer;

            for (int i = 0; i < HandData.JointCount; i++)
            {
                var joint = (TrackedHandJoint)i;
                if (joint != TrackedHandJoint.None)
                {
                    CreateJointVisualizerIfNotExists(joint);
                }
            }
        }

        private void CreateJointVisualizerIfNotExists(TrackedHandJoint handJoint)
        {
            if (jointVisualizations.TryGetValue(handJoint, out _))
            {
                return;
            }

            var prefab = jointPrefab;

            switch (handJoint)
            {
                case TrackedHandJoint.Wrist:
                    prefab = wristPrefab;
                    break;
                case TrackedHandJoint.Palm:
                    prefab = palmPrefab;
                    break;
                case TrackedHandJoint.IndexTip:
                case TrackedHandJoint.MiddleTip:
                case TrackedHandJoint.PinkyTip:
                case TrackedHandJoint.RingTip:
                case TrackedHandJoint.ThumbTip:
                    prefab = fingertipPrefab;
                    break;
            }

            if (prefab != null)
            {
                var jointVisualization = Instantiate(prefab, mainVisualizer.GetOrCreateJointTransform(handJoint));

                if (handJoint == TrackedHandJoint.IndexTip)
                {
                    var indexJointRenderer = jointVisualization.GetComponent<Renderer>();
                    var indexMaterial = indexJointRenderer.material;
                    indexMaterial.color = indexFingertipColor;
                    indexJointRenderer.material = indexMaterial;
                }

                jointVisualizations.Add(handJoint, jointVisualization);
            }
        }
    }
}