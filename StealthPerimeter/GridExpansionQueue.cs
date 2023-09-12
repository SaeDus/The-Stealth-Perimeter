namespace The_Stealth_Perimeter.StealthPerimeter
{
    using System;
    using System.Collections.Generic;

    public class GridExpansionQueue<T> where T : INodeQueueItem<T>
    {
        public List<T> ExpansionQueue => _expansionQueue;
        private List<T> _expansionQueue;

        public GridExpansionQueue()
        {
            _expansionQueue = new List<T>();
        }


        public void Add(T node)
        {
            _expansionQueue.Add(node);

            SortDown(node);
        }

        public T RemoveFirst()
        {
            var firstItem = _expansionQueue[0];

            _expansionQueue.RemoveAt(0);

            return firstItem;
        }

        public float NextCost
        {
            get
            {
                if (_expansionQueue.Count <= 0)
                    return 1000f;

                return _expansionQueue[0].ExpansionCost;
            }
        }

        public int Count { get { return _expansionQueue.Count; } }

        private void SortDown(T item)
        {
            // Assigns a handler to the next index to check
            var swapIndex = _expansionQueue.IndexOf(item) - 1;

            while (true)
            {
                // Exits the loop if the item is at the front of the queue
                if (swapIndex < 0)
                    break;

                // Assigns a handler to the next item to check against
                var swapItem = _expansionQueue[swapIndex];

                // Compares the cost of the two entries and swaps them if the new cost is less
                if (item.CompareTo(swapItem) < 0)
                    Swap(item, swapItem);

                // Exits the loop if no more swapping is needed
                else
                    break;

                // Assigns the handler to the next index to check
                swapIndex = _expansionQueue.IndexOf(item) - 1;
            }
        }

        private void Swap(T itemA, T itemB)
        {
            // Determines the indexes for each item to swap
            var indexA = _expansionQueue.IndexOf(itemA);
            var indexB = _expansionQueue.IndexOf(itemB);

            // Reassigns the list to the swapped values
            _expansionQueue[indexA] = itemB;
            _expansionQueue[indexB] = itemA;
        }
    }


    public interface INodeQueueItem<T> : IComparable<T>
    {
        float ExpansionCost { get; set; }

        List<PotentialNode> Nodes { get; set; }
    }
}