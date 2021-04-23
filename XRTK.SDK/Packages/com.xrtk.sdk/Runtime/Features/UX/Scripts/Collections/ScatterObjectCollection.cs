// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Extensions;

namespace XRTK.SDK.UX.Collections
{
    /// <summary>
    /// A Scatter Object Collection is simply a set of child objects randomly laid out within a radius.
    /// Pressing "update collection" will run the randomization, feel free to run as many times until you get the desired result.
    /// </summary>
    public class ScatterObjectCollection : GridObjectCollection
    {
        /// <summary>
        /// Overriding base function for laying out all the children when UpdateCollection is called.
        /// </summary>
        protected override void LayoutChildren()
        {
            var nodeGrid = new Vector3[nodeList.Count];

            // Now lets lay out the grid
            Columns = Mathf.CeilToInt((float)nodeList.Count / Rows);
            var startOffsetX = (Columns * 0.5f) * CellWidth;
            var startOffsetY = (Rows * 0.5f) * CellHeight;
            HalfCell = new Vector2(CellWidth * 0.5f, CellHeight * 0.5f);

            // First start with a grid then project onto surface
            ResolveGridLayout(nodeGrid, startOffsetX, startOffsetY, Layout);

            // Get randomized planar mapping
            // Calculate radius of each node while we're here
            // Then use the packer function to shift them into place
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[i];
                var newPos = nodeGrid[i].ScatterMapping(Radius);
                var nodeCollider = nodeList[i].Transform.GetComponentInChildren<Collider>();

                if (nodeCollider != null)
                {
                    // Make the radius the largest of the object's dimensions to avoid overlap
                    var bounds = nodeCollider.bounds;
                    node.Radius = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.y), bounds.size.z) * 0.5f;
                }
                else
                {
                    // Make the radius a default value
                    node.Radius = 1f;
                }

                node.Transform.localPosition = newPos;
                UpdateNodeFacing(node);
                nodeList[i] = node;
            }

            // Iterate [x] times
            for (int i = 0; i < 100; i++)
            {
                IterateScatterPacking(nodeList, Radius);
            }
        }

        /// <summary>
        /// Pack randomly spaced nodes so they don't overlap
        /// Usually requires about 25 iterations for decent packing
        /// </summary>
        private static void IterateScatterPacking(List<ObjectCollectionNode> nodes, float radiusPadding)
        {
            // Sort by closest to center (don't worry about z axis)
            // Use the position of the collection as the packing center
            nodes.Sort(ScatterSort);

            // Move them closer together
            var radiusPaddingSquared = Mathf.Pow(radiusPadding, 2f);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (i == j) { continue; }

                    var difference = nodes[j].Transform.localPosition - nodes[i].Transform.localPosition;
                    // Ignore Z axis
                    Vector2 difference2D;
                    difference2D.x = difference.x;
                    difference2D.y = difference.y;

                    var combinedRadius = nodes[i].Radius + nodes[j].Radius;
                    var distance = difference2D.SqrMagnitude() - radiusPaddingSquared;
                    var minSeparation = Mathf.Min(distance, radiusPaddingSquared);

                    distance -= minSeparation;

                    if (distance < (Mathf.Pow(combinedRadius, 2)))
                    {
                        difference2D.Normalize();
                        difference *= ((combinedRadius - Mathf.Sqrt(distance)) * 0.5f);
                        nodes[j].Transform.localPosition += difference;
                        nodes[i].Transform.localPosition -= difference;
                    }
                }
            }
        }

        private static int ScatterSort(ObjectCollectionNode circle1, ObjectCollectionNode circle2)
        {
            var distance1 = (circle1.Transform.localPosition).sqrMagnitude;
            var distance2 = (circle2.Transform.localPosition).sqrMagnitude;
            return distance1.CompareTo(distance2);
        }
    }
}
