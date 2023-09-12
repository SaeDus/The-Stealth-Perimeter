namespace The_Stealth_Perimeter.StealthPerimeter
{
    using System.Collections.Generic;
    using UnityEngine;

    public class PerimeterPool : MonoBehaviour
    {
        [Header("\tThe Node Pool"), Space(5)]
        [SerializeField, Tooltip("How many nodes to create for the initial pool.")] private int nodePoolAmount;

        [Space(10), Header("\tAgent Interaction"), Space(5)]
        [SerializeField, Tooltip("The prefab for the agent trigger nodes.")] private GameObject agentTriggerPrefab;
        [SerializeField, Tooltip("The number of agent triggers to pool.")] private int agentTriggerPoolAmount;

        [Space(10), Header("\tIllustration and Demonstration"), Space(5)]
        [SerializeField, Tooltip("The prefab to display for the active nodes.")] private GameObject activeNodeObject;
        [SerializeField, Tooltip("The prefab to display for the inactive nodes.")] private GameObject inactiveNodeObject;
        [SerializeField, Tooltip("How many objects to preallocate into the pool.")] private int objectPoolAmount;


        // The accessor for the NodePool system
        public static PerimeterPool Instance;


        // The lists that will handle the nodes and objects for each pool
        private List<PotentialNode> _nodePool;
        private List<GameObject> _activeObjectPool;
        private List<GameObject> _inactiveObjectPool;

        // The lists that will handle the used nodes and objects
        private List<PotentialNode> _usedNodePool;
        private List<GameObject> _usedActivePool;
        private List<GameObject> _usedInactivePool;

        // The list to handle the nodes the agents will deactivate when triggered
        private List<AgentTrigger> _agentNodePool;
        private List<AgentTrigger> _usedAgentPool;


#region UNITY

        private void Awake()
        {
            // Assigns the instance of the Pool if it has not been initialized
            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            // Initializes the lists so they can be used
            _nodePool = new List<PotentialNode>();
            _usedNodePool = new List<PotentialNode>();

            _activeObjectPool = new List<GameObject>();
            _usedActivePool = new List<GameObject>();

            _inactiveObjectPool = new List<GameObject>();
            _usedInactivePool = new List<GameObject>();

            _agentNodePool = new List<AgentTrigger>();
            _usedAgentPool = new List<AgentTrigger>();

            // Calls the methods to fill each pool
            FillNodePool();
            FillActiveObjectPool();
            FillInactiveObjectPool();
            FillAgentTriggerPool();
        }

#endregion


#region POOL_FILL

        /// <summary>
        /// Fills the pool of nodes with new, blank nodes.
        /// </summary>
        private void FillNodePool()
        {
            // Fills the pool with the set amount of nodes
            for (var i = 0; i < nodePoolAmount; i++)
            {
                // Creates the node for the pool
                var node = new PotentialNode();

                // Adds the newly created node to the pool
                _nodePool.Add(node);
            }
        }

        /// <summary>
        /// Fills the active pool of objects with the active prefabs.
        /// </summary>
        private void FillActiveObjectPool()
        {
            // Fills the pool with the set amount of prefabs
            for (var i = 0; i < objectPoolAmount; i++)
            {
                // Instantiates the active object for the pool and deactivates it
                var obj = Instantiate(activeNodeObject);
                obj.SetActive(false);

                // Hides the object from the hierarchy to keep that cleaned up
                obj.hideFlags = HideFlags.HideInHierarchy;

                // Adds the newly created active object to the active list
                _activeObjectPool.Add(obj);
            }
        }

        /// <summary>
        /// Fills the inactive pool of objects with the inactive prefabs.
        /// </summary>
        private void FillInactiveObjectPool()
        {
            // Fills the pool with the set amount of prefabs
            for (var i = 0; i < objectPoolAmount * 2; i++)
            {
                // Instantiates the inactive object for the pool and deactivates it
                var obj = Instantiate(inactiveNodeObject);
                obj.SetActive(false);

                // Hides the object from the hierarchy to keep that cleaned up
                obj.hideFlags = HideFlags.HideInHierarchy;

                // Adds the newly created inactive object to the inactive list
                _inactiveObjectPool.Add(obj);
            }
        }

        /// <summary>
        /// Fills the pool of agent triggers with the assigned prefabs.
        /// </summary>
        private void FillAgentTriggerPool()
        {
            // Fills the agent trigger pool with the selected number of prefabs
            for (var i = 0; i < agentTriggerPoolAmount; i++)
            {
                // Instantiates a new agent trigger object and deactivates it
                var trigger = Instantiate(agentTriggerPrefab);
                trigger.SetActive(false);

                // Hides the object from the hierarchy to keep it clean
                trigger.hideFlags = HideFlags.HideInHierarchy;

                // Adds the newly created agent trigger to the pool
                _agentNodePool.Add(trigger.GetComponent<AgentTrigger>());
            }
        }

#endregion


#region SPAWNING_FROM_POOL

        /// <summary>
        /// Provides an unused node to the calling function.
        /// </summary>
        /// <param name="worldPosition">Where to place the node in the world.</param>
        /// <param name="gridX">The X-coordinate for the node on the PotentialArea grid.</param>
        /// <param name="gridY">The Y-coordinate for the node on the PotentialArea grid.</param>
        /// <param name="gCost">The cost to move to the node from the center of the grid.</param>
        /// <returns>The newly reassigned node.</returns>
        public PotentialNode SpawnNode(Vector3 worldPosition, int gridX, int gridY, float gCost)
        {
            // If there are nodes within the pool
            if (_nodePool.Count > 0)
            {
                // Assigns a handler to the first node in the list
                var node = _nodePool[0];

                // Reassigns the values for the node
                node.ReassignNode(worldPosition, gridX, gridY, gCost);

                // Removes the node from the pool and moves it to the used pool
                _nodePool.Remove(node);
                _usedNodePool.Add(node);

                // Returns the node to the calling function
                return node;
            }

            Debug.Log("'Node Pool Amount' insufficient. Creating new node...");

            // Creates a new node
            var newNode = new PotentialNode(worldPosition, gridX, gridY, gCost);

            // Places the node directly into the used pool
            _usedNodePool.Add(newNode);

            return newNode;
        }

        /// <summary>
        /// Places an unused active object at the specified location and returns it to the calling function.
        /// </summary>
        /// <param name="spawnPosition">Where to place the object.</param>
        /// <param name="spawnScale">The starting scale for the object.</param>
        /// <returns>The unused object at the specified position and scale.</returns>
        public GameObject SpawnActiveObject(Vector3 spawnPosition, Vector3 spawnScale)
        {
            // If there are objects within the pool to return
            if (_activeObjectPool.Count > 0)
            {
                // Assigns a handler to the first object in the list
                var obj = _activeObjectPool[0];

                // Changes the position and scaling of the object and activates it
                obj.transform.position = spawnPosition;
                obj.transform.localScale = new Vector3(spawnScale.x, spawnScale.y * 0.1f, spawnScale.z);
                obj.SetActive(true);

                // Removes the object from the main active pool and moves it to the used pool
                _activeObjectPool.Remove(obj);
                _usedActivePool.Add(obj);

                // Returns the active object to the calling function
                return obj;
            }

            Debug.Log("'Object Pool Amount' insufficient. Instantiating new active object...");

            // Spawns a fresh object if the pool is completely depleted
            var newObj = Instantiate(activeNodeObject, spawnPosition, Quaternion.identity);
            newObj.transform.localScale = new Vector3(spawnScale.x, spawnScale.y * 0.1f, spawnScale.z);

            // Places the object directly into the pool of used objects
            _usedActivePool.Add(newObj);

            // Returns the new object to the calling function
            return newObj;
        }

        /// <summary>
        /// Places an unused inactive object at the specified location and returns it to the calling function.
        /// </summary>
        /// <param name="spawnPosition">Where to place the object.</param>
        /// <param name="spawnScale">The starting scale for the object.</param>
        /// <returns>The unused object at the specified position and scale.</returns>
        public GameObject SpawnInactiveObject(Vector3 spawnPosition, Vector3 spawnScale)
        {
            // If there are objects within the pool to return
            if (_inactiveObjectPool.Count > 0)
            {
                // Assigns a handler to the first object in the list
                var obj = _inactiveObjectPool[0];

                // Changes the position and scaling of the object and activates it
                obj.transform.position = spawnPosition;
                obj.transform.localScale = new Vector3(spawnScale.x, spawnScale.y * 0.1f, spawnScale.z);
                obj.SetActive(true);

                // Removes the object from the main inactive pool and moves it to the used pool
                _inactiveObjectPool.Remove(obj);
                _usedInactivePool.Add(obj);

                // Returns the inactive object to the calling function
                return obj;
            }

            Debug.Log("'Object Pool Amount' insufficient. Instantiating new inactive object...");

            // Spawns a fresh object if the pool is completely depleted
            var newObj = Instantiate(inactiveNodeObject, spawnPosition, Quaternion.identity);
            newObj.transform.localScale = new Vector3(spawnScale.x, spawnScale.y * 0.1f, spawnScale.z);

            // Places the object directly into the pool of used objects
            _usedInactivePool.Add(newObj);

            // Returns the new object to the calling function
            return newObj;
        }

        /// <summary>
        /// Provides an unused trigger to determine if an agent is investigating the area.
        /// </summary>
        /// <param name="assignedNode">The parent node to use as a reference.</param>
        /// <param name="diameter">The size of the trigger to create.</param>
        /// <param name="agentMask">The layer(s) used for the agents.</param>
        /// <returns>An unused agent trigger.</returns>
        public AgentTrigger SpawnAgentTrigger(PotentialNode assignedNode, float diameter, LayerMask agentMask)
        {
            // If there are triggers within the pool to return
            if (_agentNodePool.Count > 0)
            {
                // Assigns a handler to the first trigger in the list
                var trigger = _agentNodePool[0];

                // Assigns a handler to the trigger's transform component
                var tf = trigger.gameObject.GetComponent<Transform>();

                // Changes the position and scaling of the trigger and activates it
                tf.position = assignedNode.WorldPosition;
                tf.localScale = Vector3.one * diameter;

                // Initializes the trigger, then activates it
                trigger.InitializeTrigger(assignedNode, agentMask);
                trigger.gameObject.SetActive(true);

                // Removes the trigger from the main agent pool and moves it ot the used pool
                _agentNodePool.Remove(trigger);
                _usedAgentPool.Add(trigger);

                // Returns the new trigger to the calling function
                return trigger;
            }

            Debug.Log("'Agent Trigger Pool Amount' insufficient. Instantiating new agent trigger...");

            // Spawns a fresh trigger if the pool is dry
            var newTriggerObj = Instantiate(agentTriggerPrefab, assignedNode.WorldPosition, Quaternion.identity);
            newTriggerObj.SetActive(false);
            newTriggerObj.transform.localScale = Vector3.one * diameter;

            // Initializes the trigger to check in the correct area
            var newTrigger = newTriggerObj.GetComponent<AgentTrigger>();
            newTrigger.InitializeTrigger(assignedNode, agentMask);
            newTriggerObj.SetActive(true);

            // Places the trigger directly into the used agent trigger pool
            _usedAgentPool.Add(newTrigger);

            // Returns the newly created trigger to the calling function
            return newTrigger;
        }

#endregion


#region RETURNING_TO_POOL

        /// <summary>
        /// Handles the selected node to return it to the pool.
        /// </summary>
        /// <param name="node">The node to return to the pool.</param>
        public void RemoveNode(PotentialNode node)
        {
            // Removes the node from the used pool and moves it back to the active pool
            _usedNodePool.Remove(node);
            _nodePool.Add(node);
        }

        /// <summary>
        /// Handles the selected active object to return it to the pool.
        /// </summary>
        /// <param name="obj">The active object to return to the pool.</param>
        public void RemoveActiveObject(GameObject obj)
        {
            // Deactivates the object to return it to the pool
            obj.SetActive(false);

            // Removes the object from the used pool and moves it back to the active pool
            _usedActivePool.Remove(obj);
            _activeObjectPool.Add(obj);
        }

        /// <summary>
        /// Handles the selected inactive object to return it to the pool.
        /// </summary>
        /// <param name="obj">The inactive object to return to the pool.</param>
        public void RemoveInactiveObject(GameObject obj)
        {
            // Deactivates the object to return it to the pool
            obj.SetActive(false);

            // Removes the object from the used pool and moves it back to the inactive pool
            _usedInactivePool.Remove(obj);
            _inactiveObjectPool.Add(obj);
        }

        /// <summary>
        /// Handles the selected agent trigger to return it to the pool.
        /// </summary>
        /// <param name="trigger">The agent trigger to return to the pool.</param>
        public void RemoveAgentTrigger(AgentTrigger trigger)
        {
            // Deactivates the object to return it to the pool
            trigger.gameObject.SetActive(false);

            // Removes the trigger from the used pool and moves it back to the agent pool
            _usedAgentPool.Remove(trigger);
            _agentNodePool.Add(trigger);
        }

#endregion

    }
}