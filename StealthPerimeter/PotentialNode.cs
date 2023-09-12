namespace The_Stealth_Perimeter.StealthPerimeter
{
    using UnityEngine;

    public class PotentialNode
    {
        public Vector3 WorldPosition => _worldPosition;
        private Vector3 _worldPosition;

        public int GridX => _gridX;
        private int _gridX;

        public int GridY => _gridY;
        private int _gridY;

        // The value of the node from the starting point
        public float GCost => _gCost;
        private float _gCost;

        // Determines if the node has been investigated
        public bool Searched => _searched;
        private bool _searched;


        public PotentialNode()
        {
            _worldPosition = Vector3.zero;
            _gridX = 0;
            _gridY = 0;
            _gCost = 0;
        }

        public PotentialNode(Vector3 worldPosition, int gridX, int gridY, float gCost)
        {
            _worldPosition = worldPosition;
            _gridX = gridX;
            _gridY = gridY;
            _gCost = gCost;
        }


        public void ReassignNode(Vector3 worldPosition, int gridX, int gridY, float gCost)
        {
            _worldPosition = worldPosition;
            _gridX = gridX;
            _gridY = gridY;
            _gCost = gCost;

            _searched = false;
        }


        public void Investigate()
        {
            _searched = true;
        }
    }
}