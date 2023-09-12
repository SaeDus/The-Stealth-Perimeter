namespace The_Stealth_Perimeter.StealthPerimeter
{
    using UnityEngine;

    public class PerimeterNode
    {
        public bool Walkable => _walkable;
        private bool _walkable;

        public Vector3 WorldPosition => _worldPosition;
        private Vector3 _worldPosition;

        public int GridX => _gridX;
        private int _gridX;

        public int GridY => _gridY;
        private int _gridY;

        public int GCost;


        public PerimeterNode(bool walkable, Vector3 worldPosition, int gridX, int gridY, int gCost)
        {
            _walkable = walkable;
            _worldPosition = worldPosition;
            _gridX = gridX;
            _gridY = gridY;
            GCost = gCost;
        }
    }
}