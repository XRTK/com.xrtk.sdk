// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Utilities;
using XRTK.Extensions;

namespace XRTK.SDK.UX.Collections
{
    /// <summary>
    /// A Grid Object Collection is simply a set of child objects organized with some
    /// layout parameters.  The collection can be used to quickly create 
    /// control panels or sets of prefab/objects.
    /// </summary>
    public class GridObjectCollection : BaseObjectCollection
    {
        [SerializeField]
        [Tooltip("Type of surface to map the collection to")]
        private ObjectOrientationSurfaceType surfaceType = ObjectOrientationSurfaceType.Plane;

        /// <summary>
        /// Type of surface to map the collection to.
        /// </summary>
        public ObjectOrientationSurfaceType SurfaceType
        {
            get => surfaceType;
            set => surfaceType = value;
        }

        [SerializeField]
        [Tooltip("Should the objects in the collection be rotated / how should they be rotated")]
        private OrientationType orientType = OrientationType.None;

        /// <summary>
        /// Should the objects in the collection face the origin of the collection
        /// </summary>
        public OrientationType OrientType
        {
            get => orientType;
            set => orientType = value;
        }

        [SerializeField]
        [Tooltip("Whether to sort objects by row first or by column first")]
        private LayoutOrderType layout = LayoutOrderType.ColumnThenRow;

        /// <summary>
        /// Whether to sort objects by row first or by column first
        /// </summary>
        public LayoutOrderType Layout
        {
            get => layout;
            set => layout = value;
        }

        [SerializeField]
        [Range(0.05f, 100.0f)]
        [Tooltip("Radius for the sphere or cylinder")]
        private float radius = 2f;

        /// <summary>
        /// This is the radius of either the Cylinder or Sphere mapping and is ignored when using the plane mapping.
        /// </summary>
        public float Radius
        {
            get => radius;
            set => radius = Mathf.Clamp(value, 0.05f, 100f);
        }

        [SerializeField]
        [Tooltip("Radial range for radial layout")]
        [Range(5f, 360f)]
        private float radialRange = 180f;

        /// <summary>
        /// This is the radial range for creating a radial fan layout.
        /// </summary>
        public float RadialRange
        {
            get => radialRange;
            set => radialRange = value;
        }

        [SerializeField]
        [Range(0.0f, 1f)]
        [Tooltip("The offset for the first position in the radial layout.")]
        private float radialOffset = 0.0f;

        /// <summary>
        /// The offset for the first position in the radial layout.
        /// </summary>
        public float RadialOffset
        {
            get => radialOffset;
            set => radialOffset = value;
        }

        [SerializeField]
        [Tooltip("Number of rows per column")]
        private int rows = 3;

        /// <summary>
        /// Number of rows per column, column number is automatically determined
        /// </summary>
        public int Rows
        {
            get
            {
                if (rows <= 0)
                {
                    return rows = 1;
                }

                return rows;
            }
            set => rows = value <= 0 ? 1 : value;
        }

        [SerializeField]
        [Tooltip("Width of cell per object")]
        private float cellWidth = 0.5f;

        /// <summary>
        /// Width of the cell per object in the collection.
        /// </summary>
        public float CellWidth
        {
            get
            {
                if (cellWidth <= 0f)
                {
                    return cellWidth = 0.01f;
                }

                return cellWidth;
            }
            set => cellWidth = value <= 0f ? 0.01f : value;
        }

        [SerializeField]
        [Tooltip("Height of cell per object")]
        private float cellHeight = 0.5f;

        /// <summary>
        /// Height of the cell per object in the collection.
        /// </summary>
        public float CellHeight
        {
            get
            {
                if (cellHeight <= 0f)
                {
                    return cellHeight = 0.01f;
                }

                return cellHeight;
            }
            set => cellHeight = cellHeight <= 0f ? 0.01f : value;
        }

        /// <summary>
        /// Total Width of collection
        /// </summary>
        public float Width => Columns * CellWidth;

        /// <summary>
        /// Total Height of collection
        /// </summary>
        public float Height => Rows * CellHeight;

        /// <summary>
        /// Reference mesh to use for rendering the sphere layout
        /// </summary>
        public Mesh SphereMesh { get; set; }

        /// <summary>
        /// Reference mesh to use for rendering the cylinder layout
        /// </summary>
        public Mesh CylinderMesh { get; set; }

        protected int Columns;

        protected Vector2 HalfCell;

        /// <summary>
        /// Overriding base function for laying out all the children when UpdateCollection is called.
        /// </summary>
        protected override void LayoutChildren()
        {
            Vector3 newPos;
            var nodeGrid = new Vector3[nodeList.Count];

            // Now lets lay out the grid
            Columns = Mathf.CeilToInt((float)nodeList.Count / Rows);
            var startOffsetX = (Columns * 0.5f) * CellWidth;
            var startOffsetY = (Rows * 0.5f) * CellHeight;
            HalfCell = new Vector2(CellWidth * 0.5f, CellHeight * 0.5f);

            // First start with a grid then project onto surface
            ResolveGridLayout(nodeGrid, startOffsetX, startOffsetY, layout);

            switch (SurfaceType)
            {
                case ObjectOrientationSurfaceType.Plane:
                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        var node = nodeList[i];
                        newPos = nodeGrid[i];
                        node.Transform.localPosition = newPos;
                        UpdateNodeFacing(node);
                        nodeList[i] = node;
                    }
                    break;

                case ObjectOrientationSurfaceType.Cylinder:
                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        var node = nodeList[i];
                        newPos = VectorExtensions.CylindricalMapping(nodeGrid[i], radius);
                        node.Transform.localPosition = newPos;
                        UpdateNodeFacing(node);
                        nodeList[i] = node;
                    }
                    break;

                case ObjectOrientationSurfaceType.Sphere:

                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        var node = nodeList[i];
                        newPos = VectorExtensions.SphericalMapping(nodeGrid[i], radius);
                        node.Transform.localPosition = newPos;
                        UpdateNodeFacing(node);
                        nodeList[i] = node;
                    }
                    break;

                case ObjectOrientationSurfaceType.Radial:
                    int curColumn = 0;
                    int curRow = 1;

                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        var node = nodeList[i];
                        newPos = VectorExtensions.RadialMapping(nodeGrid[i], radialRange, radius, curRow, Rows, curColumn, Columns, radialOffset);

                        if (curColumn == (Columns - 1))
                        {
                            curColumn = 0;
                            ++curRow;
                        }
                        else
                        {
                            ++curColumn;
                        }

                        node.Transform.localPosition = newPos;
                        UpdateNodeFacing(node);
                        nodeList[i] = node;
                    }
                    break;
            }
        }

        protected void ResolveGridLayout(Vector3[] grid, float offsetX, float offsetY, LayoutOrderType order)
        {
            float iMax;
            float jMax;
            int cellCounter = 0;

            if (order == LayoutOrderType.RowThenColumn)
            {
                iMax = Rows;
                jMax = Columns;
            }
            else
            {
                iMax = Columns;
                jMax = Rows;
            }

            for (int i = 0; i < iMax; i++)
            {
                for (int j = 0; j < jMax; j++)
                {
                    if (cellCounter < nodeList.Count)
                    {
                        var width = ((i * CellWidth) - offsetX + HalfCell.x) + nodeList[cellCounter].Offset.x;
                        var height = (-(j * CellHeight) + offsetY - HalfCell.y) + nodeList[cellCounter].Offset.y;
                        grid[cellCounter].Set(width, height, 0.0f);
                    }
                    cellCounter++;
                }
            }
        }

        /// <summary>
        /// Update the facing of a node given the nodes new position for facing origin with node and orientation type
        /// </summary>
        /// <param name="node"></param>
        protected void UpdateNodeFacing(ObjectCollectionNode node)
        {
            Vector3 centerAxis;
            Vector3 pointOnAxisNearestNode;

            var up = transform.up;
            var position = transform.position;
            var nodePosition = node.Transform.position;

            switch (OrientType)
            {
                case OrientationType.FaceOrigin:
                    node.Transform.rotation = Quaternion.LookRotation(nodePosition - position, up);
                    break;

                case OrientationType.FaceOriginReversed:
                    node.Transform.rotation = Quaternion.LookRotation(position - nodePosition, up);
                    break;

                case OrientationType.FaceCenterAxis:
                    centerAxis = Vector3.Project(node.Transform.position - position, up);
                    pointOnAxisNearestNode = position + centerAxis;
                    node.Transform.rotation = Quaternion.LookRotation(nodePosition - pointOnAxisNearestNode, up);
                    break;

                case OrientationType.FaceCenterAxisReversed:
                    centerAxis = Vector3.Project(node.Transform.position - position, up);
                    pointOnAxisNearestNode = position + centerAxis;
                    node.Transform.rotation = Quaternion.LookRotation(pointOnAxisNearestNode - nodePosition, up);
                    break;

                case OrientationType.FaceParentFoward:
                    node.Transform.forward = transform.rotation * Vector3.forward;
                    break;

                case OrientationType.FaceParentForwardReversed:
                    node.Transform.forward = transform.rotation * Vector3.back;
                    break;

                case OrientationType.FaceParentUp:
                    node.Transform.forward = transform.rotation * Vector3.up;
                    break;

                case OrientationType.FaceParentDown:
                    node.Transform.forward = transform.rotation * Vector3.down;
                    break;

                case OrientationType.None:
                    break;

                default:
                    Debug.LogWarning("OrientationType out of range");
                    break;
            }
        }

        // Gizmos to draw when the Collection is selected.
        protected virtual void OnDrawGizmosSelected()
        {
            var scale = (2f * radius) * Vector3.one;
            var rotation = transform.rotation;

            switch (surfaceType)
            {
                case ObjectOrientationSurfaceType.Plane:
                    break;
                case ObjectOrientationSurfaceType.Cylinder:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireMesh(CylinderMesh, transform.position, rotation, scale);
                    break;
                case ObjectOrientationSurfaceType.Sphere:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireMesh(SphereMesh, transform.position, rotation, scale);
                    break;
                case ObjectOrientationSurfaceType.Radial:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireMesh(SphereMesh, transform.position, rotation, scale);
                    break;
            }
        }
    }
}