namespace The_Stealth_Perimeter.StealthPerimeter
{
    using System.Collections.Generic;

    public class ExpansionEntry : INodeQueueItem<ExpansionEntry>
    {
        private float _expansionCost;
        public float ExpansionCost { get { return _expansionCost; } set { _expansionCost = value; } }

        private List<PotentialNode> _nodes;
        public List<PotentialNode> Nodes { get { return _nodes; } set { _nodes = value; } }

        public int Count { get { return _nodes.Count; } }

        public int CompareTo(ExpansionEntry entryToCompare)
        {
            return _expansionCost.CompareTo(entryToCompare.ExpansionCost);
        }


        public ExpansionEntry()
        {
            _nodes = new List<PotentialNode>();
        }


        public void Add(PotentialNode node)
        {
            _nodes.Add(node);
            _expansionCost = node.GCost;
        }

        public void Remove(PotentialNode node)
        {
            _nodes.Remove(node);
        }
    }
}