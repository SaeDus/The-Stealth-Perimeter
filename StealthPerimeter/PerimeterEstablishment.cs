namespace The_Stealth_Perimeter.StealthPerimeter
{
    using System.Collections.Generic;
    using UnityEngine;

    public class PerimeterEstablishment : MonoBehaviour
    {
        // Variables needed to establish the full grid information
        private Vector2 _gridWorldSize;
        private float _nodeRadius;
        private float _targetRadius;
        private LayerMask _walkableMask;
        private LayerMask _unwalkableMask;

        // Calculated values from provided variables
        private float _nodeDiameter;
        private int _gridSizeX;
        private int _gridSizeY;


        // The information for the grid is stored in these two dictionaries
        private Dictionary<Vector2, bool> _walkableGrid;
        private Dictionary<Vector2, Vector3> _positionGrid;


        // Establishes the static instance for the singleton
        public static PerimeterEstablishment Instance;


#region UNITY

        private void Awake()
        {
            // Ensures the singleton is setup properly
            if (Instance != null && Instance != this)
                Destroy(this);

            Instance = this;
        }

#endregion


#region GRID_ESTABLISHMENT

        /// <summary>
        /// Initializes the class, then scans the map to fill out the information for a grid of the map.
        /// </summary>
        /// <param name="gridWorldSize">The size of the map to cover.</param>
        /// <param name="nodeRadius">The size of each node in the grid.</param>
        /// <param name="targetRadius">The size of the target.</param>
        /// <param name="walkableMask">The walkable layer(s) the target can traverse.</param>
        /// <param name="unwalkableMask">The unwalkable layer(s) the target cannot traverse.</param>
        public void EstablishPerimeterGrid(
            Vector2 gridWorldSize, float nodeRadius, float targetRadius, LayerMask walkableMask, LayerMask unwalkableMask)
        {
            // Ensures all values are assigned to the class before establishing the grid
            _gridWorldSize = gridWorldSize;
            _nodeRadius = nodeRadius;
            _targetRadius = targetRadius;
            _walkableMask = walkableMask;
            _unwalkableMask = unwalkableMask;

            // Calculates the other variables based on the imported values
            _nodeDiameter = _nodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(_gridWorldSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(_gridWorldSize.y / _nodeDiameter);

            // Calls the methods to establish the full grid
            CreateGrid();
        }

        /// <summary>
        /// Handles creating the grid and filling out all necessary information for each node.
        /// </summary>
        private void CreateGrid()
        {
            // Initializes the dictionaries to use for the full grid information
            _walkableGrid = new Dictionary<Vector2, bool>();
            _positionGrid = new Dictionary<Vector2, Vector3>();

            // Determines the position of the bottom-left corner of the grid.
            var worldBottomLeft = transform.position
                - Vector3.right * _gridWorldSize.x / 2
                - Vector3.forward * _gridWorldSize.y / 2;

            // Cycles through the x-coordinates
            for (var x = 0; x < _gridSizeX; x++)
            {
                // Cycles through the y-coordinates
                for (var y = 0; y < _gridSizeY; y++)
                {
                    // Creates the key for the grid position
                    var gridPos = new Vector2(x, y);

                    // Determines the position in the world for the new node
                    var worldPoint = worldBottomLeft
                        + Vector3.right * (x * _nodeDiameter + _nodeRadius)
                        + Vector3.forward * (y * _nodeDiameter + _nodeRadius);

                    // Creates a handler to determine if the area is walkable
                    var walkable = false;

                    // Checks if there is an obstacle only if the floor is walkable
                    if (Physics.CheckSphere(worldPoint, _nodeRadius, _walkableMask))
                        walkable = !Physics.CheckSphere(worldPoint, _nodeRadius, _unwalkableMask);

                    // Checks if there is an obstacle that would prevent the target from walking here
                    if (walkable)
                        walkable = !Physics.CheckSphere(worldPoint, _targetRadius, _unwalkableMask);

                    // Adds the new information for the grid to the dictionaries
                    _walkableGrid.Add(gridPos, walkable);
                    _positionGrid.Add(gridPos, worldPoint);
                }
            }
        }

#endregion


#region GRID_ACCESS

        /// <summary>
        /// Checks the list to see if the selected area is walkable.
        /// </summary>
        /// <param name="worldPosition">Where to check for a walkable area.</param>
        /// <returns>Is the area walkable?</returns>
        public bool CheckWalkableArea(Vector3 worldPosition)
        {
            // Calls the method to return the walkable status of the appropriate node
            return _walkableGrid[GetNodeFromWorldPoint(worldPosition)];
        }

        /// <summary>
        /// Checks the list to get the node's world position from the list.
        /// </summary>
        /// <param name="worldPosition">Where to check for a node's position.</param>
        /// <returns>The node's position in world space.</returns>
        public Vector3 GetNodePositionFromWorldPoint(Vector3 worldPosition)
        {
            // Calls the method to return the world position of the appropriate node
            return _positionGrid[GetNodeFromWorldPoint(worldPosition)];
        }


        /// <summary>
        /// Gets the appropriate node from the grid based on the imported world position.
        /// </summary>
        /// <param name="worldPosition">Where to check for a node.</param>
        /// <returns>The key for the necessary node in either dictionary.</returns>
        private Vector2 GetNodeFromWorldPoint(Vector3 worldPosition)
        {
            // Calculates how close the point is to either end of either grid axis
            var percentX = (worldPosition.x + _gridWorldSize.x / 2) / _gridWorldSize.x;
            var percentY = (worldPosition.z + _gridWorldSize.y / 2) / _gridWorldSize.y;

            // Clamps the values between 0 and 1
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            // Determines the int value to determine which node within which the position falls
            var x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
            var y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

            // Returns the position of the closest node
            return new Vector2(x, y);
        }

#endregion


        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(_gridWorldSize.x, 1, _gridWorldSize.y));
        }
    }
}
