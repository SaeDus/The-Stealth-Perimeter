namespace The_Stealth_Perimeter.StealthPerimeter
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [RequireComponent(typeof(PerimeterEstablishment))]
    public class PotentialArea : MonoBehaviour
    {
        [Header("\tScene Objects"), Space(5)]
        [SerializeField, Tooltip("The main camera that will check for mouse clicks.")] private Camera mainCamera;

        [Space(10), Header("\tNode Information"), Space(5)]
        [SerializeField, Tooltip("The full size of the map to cover.")] private Vector2 gridWorldSize;
        [SerializeField, Tooltip("How large to make the nodes for the grid.")] private float nodeRadius;
        [SerializeField, Tooltip("The layer(s) considered walkable for the agents.")] private LayerMask walkableMask;
        [SerializeField, Tooltip("The layer(s) considered unwalkable for the agents.")] private LayerMask unwalkableMask;

        [Space(10), Header("\tTarget Information"), Space(5)]
        [SerializeField, Tooltip("The size of the player.")] private float targetRadius;
        [SerializeField, Tooltip("The speed of the player.")] private float targetSpeed;

        [Space(10), Header("\tAgent Information"), Space(5)]
        [SerializeField, Tooltip("The layer(s) for the agents.")] private LayerMask agentMask;
        [SerializeField] private GameObject testAgent;

        [Space(10), Header("\tIllustration"), Space(5)]
        [SerializeField, Tooltip("The prefab for the illustration to create.")] private GameObject stealthIllustration;

        [Space(20), Header("\tGizmos and Testing"), Space(5)]
        [Tooltip("If the area should be drawn.")] public bool ShowPotentialArea;
        [HideInInspector, Tooltip("The Gizmo color for the active nodes.")] public Color ActiveColor;
        [HideInInspector, Tooltip("The Gizmo color for the inactive nodes.")] public Color InactiveColor;


        // Creates a static handler for the Perimeter so locations can be retrieved quickly
        public static PotentialArea Instance;


        // Constant values for moving along the grid
        private const float AdjacentValue = 10f;
        private const float CornerValue = 14f;


        // Handlers for the nodes in the grid to determine how to expand the perimeter
        private List<PotentialNode> _grid;
        private GridExpansionQueue<ExpansionEntry> _outsideExpansionQueue;
        private GridExpansionQueue<ExpansionEntry> _insideExpansionQueue;
        private List<AgentTrigger> _agentNodes;

        // Determines the index for each node on the grid to avoid searching through the entire grid
        private Dictionary<Vector2, int> _gridIndexes;

        // The illustrative objects to display the perimeter
        private GameObject _stealthObject;
        private List<GameObject> _activeObjects;
        private List<GameObject> _inactiveObjects;
        private List<PotentialNode> _buriedNodes;


        // Determines the diameter for the nodes on the grid
        private float _nodeDiameter;


        // Handles the coroutines to start/stop as needed
        private IEnumerator _expansionCoroutine;


        // Determines the diameter for the player to handle the trigger objects
        private float _targetDiameter;

        // Handler for quickly determining the calculation for the rate of expansion
        private float _expansionRate;

        // Determines the movement potential for the stealth agent
        private float _movementPotential;

        // Timers for handling when to check for neighbors
        private float _insideTimer;
        private float _outsideTimer;


#region UNITY

        private void Awake()
        {
            // Ensures the singleton is setup properly
            if (Instance != null && Instance != this)
                Destroy(this);

            Instance = this;

            // Pools the stealth object illustration by creating it at the start
            _stealthObject = Instantiate(stealthIllustration);
            _stealthObject.SetActive(false);
            _stealthObject.hideFlags = HideFlags.HideInInspector;

            // Calculates the diameters for the variables
            _nodeDiameter = nodeRadius * 2;
            _targetDiameter = targetRadius * 2;

            // Calculates the rate of expansion
            _expansionRate = targetRadius / nodeRadius * targetSpeed;
        }

        private void Start()
        {
            // Calls the method to establish the full grid for the perimeter
            PerimeterEstablishment.Instance.EstablishPerimeterGrid(gridWorldSize, nodeRadius, targetRadius, walkableMask, unwalkableMask);
        }

        // *For testing purposes
        //private void Update()
        //{
        //    // Checks if the player presses the left mouse button
        //    if (Input.GetMouseButtonDown(0))
        //    {
        //        // Creates the starting point on the main camera to cast a ray for the mouse click
        //        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        //        // Performs the raycast to get the position of where the player was clicking
        //        if (Physics.Raycast(ray, out var hit))

        //            // Calls the method with the raycast information to start the perimeter system
        //            StartPotentialGrid(hit.point);
        //    }

        //    // Checks if the player presses the space bar
        //    if (Input.GetKeyDown(KeyCode.Space))
        //    {
        //        // Calls the method to stop expanding the perimeter
        //        StopGridExpansion();
        //    }
        //}

#endregion


#region POTENTIAL_AREA_INITIALIZATION

        /// <summary>
        /// Resets all lists and prefabs for the perimeter to start over.
        /// </summary>
        public void ClearPotentialArea()
        {
            // Calls the NodePool method to remove all nodes from the grid
            if (_grid != null)
                foreach (var n in _grid)
                    PerimeterPool.Instance.RemoveNode(n);

            // Reinitializes the lists for the nodes
            _grid = new List<PotentialNode>();
            _outsideExpansionQueue = new GridExpansionQueue<ExpansionEntry>();
            _insideExpansionQueue = new GridExpansionQueue<ExpansionEntry>();

            // Resets the index dictionary for the grid
            _gridIndexes = new Dictionary<Vector2, int>();

            // Stops the expansion coroutine if it is running
            if (_expansionCoroutine != null)
                StopCoroutine(_expansionCoroutine);

            // Deactivates the stealth illustration object
            _stealthObject.SetActive(false);

            // Resets the movement potential for the player
            _movementPotential = 0;

            // Resets the timers for checking the neighbors
            _insideTimer = 0;
            _outsideTimer = 0;

            /* Illustration Reinitialization */

            // Calls the NodePool method to remove the object for any objects within the active list
            if (_activeObjects != null)
                foreach (var obj in _activeObjects)
                    PerimeterPool.Instance.RemoveActiveObject(obj);

            // Calls the NodePool method to remove the object for any objects within the inactive list
            if (_inactiveObjects != null)
                foreach (var obj in _inactiveObjects)
                    PerimeterPool.Instance.RemoveInactiveObject(obj);

            if (_agentNodes != null)
                foreach (var t in _agentNodes)
                    PerimeterPool.Instance.RemoveAgentTrigger(t);

            // Reinitializes all lists so they can be used again
            _activeObjects = new List<GameObject>();
            _inactiveObjects = new List<GameObject>();
            _buriedNodes = new List<PotentialNode>();
            _agentNodes = new List<AgentTrigger>();
        }

        /// <summary>
        /// Establishes the origin point for the perimeter and ensures it starts at the same size as the target.
        /// </summary>
        /// <param name="origin">Where to begin the perimeter.</param>
        private void CreateStartingNodes(Vector3 origin)
        {
            // Utilizes the PerimeterPool to create the first node for the origin point from which to start expanding
            var newNode = PerimeterPool.Instance.SpawnNode(origin, 0, 0, 0);

            // Adds the node ot the necessary lists for handling further
            _grid.Add(newNode);

            // Creates a new ExpansionEntry object for the new node
            var newEntry = new ExpansionEntry();

            // Adds the origin node to the entry
            newEntry.Add(newNode);

            // Adds the new entry to the outside expansion queue
            _outsideExpansionQueue.Add(newEntry);

            // If the initial area needs to be expanded to reach the size of the player
            if (nodeRadius < targetRadius)
            {
                // Calculates the starting movement potential
                _movementPotential = targetRadius / nodeRadius * 5;

                // Starts the coroutine to create all neighboring nodes up to the movement potential
                StartCoroutine(nameof(CreatePotentialNeighbors));
            }
        }

#endregion


#region POTENTIAL_EXPANSION

        /// <summary>
        /// Starts the perimeter system by setting the starting point.
        /// </summary>
        /// <param name="worldPosition">Where to start the perimeter.</param>
        public void StartPotentialGrid(Vector3 worldPosition)
        {
            // Exits if the world position is not walkable
            if (!PerimeterEstablishment.Instance.CheckWalkableArea(worldPosition))
                return;

            // Determines the true origin for the perimeter snapped to the grid
            var origin = PerimeterEstablishment.Instance.GetNodePositionFromWorldPoint(worldPosition);

            // Calls the method to clear any existing perimeter system
            ClearPotentialArea();

            // Moves the stealth illustration object and activates it
            _stealthObject.transform.position = origin;
            _stealthObject.SetActive(true);

            // Calls the method to create the starting node(s)
            CreateStartingNodes(origin);

            // Sets the timers based on the next time each queue will need to expand
            _insideTimer = _insideExpansionQueue.NextCost;
            _outsideTimer = _outsideExpansionQueue.NextCost;

            // Starts the coroutine to handle expanding the perimeter
            _expansionCoroutine = ExpandPotentialArea();
            StartCoroutine(_expansionCoroutine);

            // Calls the method to setup the prefabs for displaying the perimeter
            RefreshIllustratedArea();
        }

        /// <summary>
        /// Expands the potential area over time by increasing the movement potential.
        /// </summary>
        private IEnumerator ExpandPotentialArea()
        {
            // Loops until the coroutine is manually stopped
            while (true)
            {
                // Increments the movement potential over time based on the player speed
                _movementPotential += _expansionRate * Time.deltaTime;

                // If the inside timer has reached its limit
                if (_movementPotential >= _insideTimer)
                {
                    // Ensures the check is only made once
                    if (_insideTimer < _movementPotential)
                    {
                        // Sets the timer too high to avoid calling again until needed
                        _insideTimer = _movementPotential + 1000f;

                        // Starts the coroutine to create the neighbors
                        StartCoroutine(nameof(CreatePotentialNeighbors));
                    }
                }

                // If the outside timer has reached its limitt
                if (_movementPotential >= _outsideTimer)
                {
                    // Ensures the check is only made once
                    if (_outsideTimer < _movementPotential)
                    {
                        // Sets the timer too high to avoid calling again until needed
                        _outsideTimer = _movementPotential + 1000f;

                        // Starts the coroutine to create the neighbors
                        StartCoroutine(nameof(CreatePotentialNeighbors));
                    }
                }

                // Pauses the coroutine for a frame to continue the grid expansion
                yield return null;
            }
        }

        /// <summary>
        /// Handles creating all neighbors based on the current movement potential value.
        /// </summary>
        private IEnumerator CreatePotentialNeighbors()
        {
            // Creates a handler to determine when to stop looping through the neighbors
            var keepChecking = true;

            // Loops until the lists have been evaluated
            while (keepChecking)
            {
                // Calls the method to check the inside nodes list
                var insideChange = ExpandInsideQueueNeighbors();

                // Calls the method to check the outside nodes list
                var outsideChange = ExpandOutsideQueueNeighbors();

                // Exits if no changes were made from either list
                if (!insideChange && !outsideChange)
                    keepChecking = false;
            }

            // Calls the method to setup the prefabs for displaying the perimeter
            RefreshIllustratedArea();

            // Sets the timers based on the next time each queue will need to expand
            _insideTimer = _insideExpansionQueue.NextCost;
            _outsideTimer = _outsideExpansionQueue.NextCost;

            // Calls the method to stop expanding the grid if both queues are empty
            if (_insideExpansionQueue.Count <= 0 && _outsideExpansionQueue.Count <= 0)
                StopGridExpansion();

            // Exits the coroutine
            yield return null;
        }

        /// <summary>
        /// Cycles through the inside list of nodes to create any corner neighbors.
        /// </summary>
        /// <returns>Did anything change with the inside list?</returns>
        private bool ExpandInsideQueueNeighbors()
        {
            // Exits if the inside expansion queue is not ready to expand
            if (_movementPotential < _insideExpansionQueue.NextCost)
                return false;

            // Exits if there are no entries in the inside expansion queue
            if (_insideExpansionQueue.Count <= 0)
                return false;

            // Retrieves and removes the first entry from the inside expansion queue
            var oldInsideEntry = _insideExpansionQueue.RemoveFirst();

            // Creates a new expansion entry for any new nodes
            var newOutsideEntry = new ExpansionEntry();

            // Cycles through each node in the next queue entry
            foreach (var n in oldInsideEntry.Nodes)
            {
                // Skips the node if it has been searched
                if (n.Searched)
                    continue;

                // Cycles through the x-coordinates around the node
                for (var x = -1; x <= 1; x++)
                {
                    // Cycles through the y-coordinates around the node
                    for (var y = -1; y <= 1; y++)
                    {
                        // Skips the coordinates if they are not selecting a corner neighbor
                        if (x == 0 || y == 0)
                            continue;

                        // Skips the node if the grid already contains a node at the assigned coordinates
                        if (_gridIndexes.ContainsKey(new Vector2(n.GridX + x, n.GridY + y)))
                            continue;

                        // Determines the new world position for the new node to create
                        var newPosition = new Vector3(
                            n.WorldPosition.x + x * _nodeDiameter,
                            n.WorldPosition.y,
                            n.WorldPosition.z + y * _nodeDiameter);


                        // Skips the node if its area is unwalkable
                        if (!PerimeterEstablishment.Instance.CheckWalkableArea(newPosition))
                            continue;

                        // Utilizes the PerimeterPool to create a new node for the grid
                        var newNode = PerimeterPool.Instance.SpawnNode(newPosition, n.GridX + x, n.GridY + y, n.GCost + CornerValue);

                        // Adds the new node to the new entry
                        newOutsideEntry.Add(newNode);

                        // Calls the method to add the new node to the grid
                        AddNewNodeToGrid(newNode);
                    }
                }
            }

            // If any nodes were added to the entry
            if (newOutsideEntry.Count > 0)
            {
                // Adds the entry to the outside expansion queue
                _outsideExpansionQueue.Add(newOutsideEntry);

                // Calls the method to create agent triggers for the list of nodes
                CreateAgentTriggersForList(newOutsideEntry.Nodes);

                // Assigns the timer to the next entry in the outside queue
                _outsideTimer = _outsideExpansionQueue.NextCost;
            }

            // Assigns the timer to the next entry in the inside queue
            _insideTimer = _insideExpansionQueue.NextCost;

            // Informs the calling function there was a change made to the inside expansion queue
            return true;
        }

        /// <summary>
        /// Cycles through the outside list of nodes to create any adjacent neighbors.
        /// </summary>
        /// <returns>Did anything change with the outside list?</returns>
        private bool ExpandOutsideQueueNeighbors()
        {
            // Exits if the outside expansion queue is not ready to expand
            if (_movementPotential < _outsideExpansionQueue.NextCost)
                return false;

            // Exits if there are no entries in the outside expansion queue
            if (_outsideExpansionQueue.Count <= 0)
                return false;

            // Retrieves and removes the next entry from the outside expansion queue
            var oldOutsideList = _outsideExpansionQueue.RemoveFirst();

            // Creates a new expansion entry for any new nodes
            var newOutsideList = new ExpansionEntry();

            // Cycles through each node in the next queue entry
            foreach (var n in oldOutsideList.Nodes)
            {
                // Skips the node if it has been searched
                if (n.Searched)
                    continue;

                // Cycles through the x-coordinates around the node
                for (var x = -1; x <= 1; x++)
                {
                    // Skips the coordinates if they are selecting the node being checked
                    if (x == 0)
                        continue;

                    // Skips the coordinates if a node has already been created there
                    if (_gridIndexes.ContainsKey(new Vector2(n.GridX + x, n.GridY)))
                        continue;

                    // Determines the world position for the new node
                    var newPosition = n.WorldPosition;
                    newPosition.x += x * _nodeDiameter;

                    // Skips the node if its area is unwalkable
                    if (!PerimeterEstablishment.Instance.CheckWalkableArea(newPosition))
                        continue;

                    // Utilizes the PerimeterPool to create a new node for the grid
                    var newNode = PerimeterPool.Instance.SpawnNode(newPosition, n.GridX + x, n.GridY, n.GCost + AdjacentValue);

                    // Adds the new node to the entry
                    newOutsideList.Add(newNode);

                    // Calls the method to add the new node to the grid
                    AddNewNodeToGrid(newNode);
                }

                // Cycles through the y-coordinates around the node
                for (var y = -1; y <= 1; y++)
                {
                    // Skips the coordinates if they are selecting the node being checked
                    if (y == 0)
                        continue;

                    // Skips the coordinates if a node has already been created there
                    if (_gridIndexes.ContainsKey(new Vector2(n.GridX, n.GridY + y)))
                        continue;

                    // Determine the world position for the new node
                    var newPosition = n.WorldPosition;
                    newPosition.z += y * _nodeDiameter;

                    // Skips the node if its area is unwalkable
                    if (!PerimeterEstablishment.Instance.CheckWalkableArea(newPosition))
                        continue;

                    // Utilizes the PerimeterPool to create a new node for the grid
                    var newNode = PerimeterPool.Instance.SpawnNode(newPosition, n.GridX, n.GridY + y, n.GCost + AdjacentValue);

                    // Adds the node to the entry
                    newOutsideList.Add(newNode);

                    // Calls the method to add the new node to the grid
                    AddNewNodeToGrid(newNode);
                }
            }

            // Adds the old outside list to the inside expansion queue
            _insideExpansionQueue.Add(oldOutsideList);

            // Calls the method to remove the agent triggers from the old outside nodes
            RemoveAllAgentTriggersFromNodes(oldOutsideList.Nodes);

            // If any nodes were added to the entry
            if (newOutsideList.Count > 0)
            {
                // Adds the entry to the outside expansion queue
                _outsideExpansionQueue.Add(newOutsideList);

                // Calls the method to create agent triggers for the new outside nodes
                CreateAgentTriggersForList(newOutsideList.Nodes);
            }

            // Assigns the timers to the next entries in their respective queues
            _insideTimer = _insideExpansionQueue.NextCost;
            _outsideTimer = _outsideExpansionQueue.NextCost;

            // Informs the calling function a change was made to the outside expansion queue
            return true;
        }

        /// <summary>
        /// Adds a new node to the grid and assigns all values.
        /// </summary>
        /// <param name="node">The newly created node.</param>
        private void AddNewNodeToGrid(PotentialNode node)
        {
            // Determines the index for the new node and adds it to the dictionary
            _gridIndexes.Add(new Vector2(node.GridX, node.GridY), _grid.Count);

            // Adds the node to the grid
            _grid.Add(node);
        }

        /// <summary>
        /// Handles stopping the perimeter from expanding any further.
        /// </summary>
        public void StopGridExpansion()
        {
            // Stops the expansion coroutine if it is running
            if (_expansionCoroutine != null)
                StopCoroutine(_expansionCoroutine);
        }

#endregion


#region AGENT_INVESTIGATION

        /// <summary>
        /// Handles creating agent triggers for a list of nodes.
        /// </summary>
        /// <param name="nodeList">The list of nodes that need attached agent triggers.</param>
        private void CreateAgentTriggersForList(List<PotentialNode> nodeList)
        {
            // Cycles through the list of nodes and calls the method to create a new agent trigger
            foreach (var n in nodeList)
                CreateAgentTriggerForNode(n);
        }

        /// <summary>
        /// Creates a single agent trigger for an assigned node.
        /// </summary>
        /// <param name="node">The node to assign to a new agent trigger.</param>
        private void CreateAgentTriggerForNode(PotentialNode node)
        {
            // Cycles through the existing agent triggers and exits if one already exists for the selected node
            foreach (var t in _agentNodes)
                if (t.AssignedNode == node)
                    return;

            // Utilizes the PerimeterPool to create a new agent trigger and adds it to the list
            var newTrigger = PerimeterPool.Instance.SpawnAgentTrigger(node, _nodeDiameter * 1.1f, agentMask);
            _agentNodes.Add(newTrigger);
        }

        /// <summary>
        /// Handles removing all agent triggers from a list of nodes.
        /// </summary>
        /// <param name="nodes">The nodes from which the agent triggers need to be removed.</param>
        private void RemoveAllAgentTriggersFromNodes(List<PotentialNode> nodes)
        {
            // Cycles through the imported list of nodes
            foreach (var n in nodes)
            {
                // Determines if the agent trigger needs to be removed
                var removeNode = true;

                // Calls the methods to gather lists of neighbors for the node
                var adjacent = GetAdjacentNeighborNodes(n);
                var corner = GetCornerNeighborNodes(n);

                // If the node has adjacent neighbors
                if (adjacent.Count > 0)
                {
                    // Cycles through the list of adjacent neighbors
                    foreach (var a in adjacent)
                    {
                        // If the neighbor has been investigated by an agent
                        if (a.Searched)
                        {
                            // Informs the handler the agent trigger should not be removed
                            removeNode = false;

                            // Stops cycling through the adjacent neighbors
                            break;
                        }
                    }
                }

                // If the node is still set to be removed and has corner neighbors
                if (removeNode && corner.Count > 0)
                {
                    // Cycles through the list of corner neighbors
                    foreach (var c in corner)
                    {
                        // If the neighbor has been investigated by an agent
                        if (c.Searched)
                        {
                            // Informs the handler the agent trigger should not be removed
                            removeNode = false;

                            // Stops cycling through the list of corner neighbors
                            break;
                        }
                    }
                }

                // Calls the method to remove the agent trigger if it is still designated as such
                if (removeNode)
                    RemoveAgentTriggerFromNode(n);
            }
        }

        /// <summary>
        /// Removes an agent trigger from an assigned node.
        /// </summary>
        /// <param name="node">The node from which an agent trigger needs to be removed.</param>
        private void RemoveAgentTriggerFromNode(PotentialNode node)
        {
            // Cycles through the list of agent triggers
            foreach (var t in _agentNodes)
            {
                // If an agent trigger is found that is assigned to the imported node
                if (t.AssignedNode == node)
                {
                    // Utilizes the PerimeterPool to return the agent trigger to the pool
                    PerimeterPool.Instance.RemoveAgentTrigger(t);

                    // Removes the agent trigger from the list
                    _agentNodes.Remove(t);

                    // Exits the function after completion to avoid cycling to the end of the list
                    return;
                }
            }
        }

        /// <summary>
        /// Handles investigating a node when triggered.
        /// </summary>
        /// <param name="agentNode">The agent trigger that was investigated by an agent.</param>
        public void NodeInvestigated(AgentTrigger agentNode)
        {
            // Starts the coroutine to handle the functionality for investigating the node
            HandleNodeInvestigation(agentNode);
        }

        /// <summary>
        /// Handles all changes for when a node is investigated.
        /// </summary>
        /// <param name="agentNode">The node that has been investigated.</param>
        private void HandleNodeInvestigation(AgentTrigger agentNode)
        {
            // Calls the methods to get all neighbors for the selected node
            var adjacentNodes = GetAdjacentNeighborNodes(agentNode.AssignedNode);
            var cornerNodes = GetCornerNeighborNodes(agentNode.AssignedNode);

            // Calls the method to create new agent triggers for any adjacent neighbors
            if (adjacentNodes.Count > 0)
                CreateAgentTriggersForList(adjacentNodes);

            // Calls the method to create new agent triggers for any corner neighbors
            if (cornerNodes.Count > 0)
                CreateAgentTriggersForList(cornerNodes);

            // Calls the methods to deactivate the investigated node and remove the assigned agent trigger
            DeactivateNode(agentNode.AssignedNode);
            RemoveAgentTrigger(agentNode);
        }

        /// <summary>
        /// Deactivates a node by marking it is having been searched.
        /// </summary>
        /// <param name="investigatedNode">The node that has been investigated.</param>
        private void DeactivateNode(PotentialNode investigatedNode)
        {
            // Informs the node it has been investigated by an agent
            investigatedNode.Investigate();

            // Calls the method to remove the illustration for the selected node
            RemoveIllustration(investigatedNode);
        }

        /// <summary>
        /// Scans a node to retrieve all adjacent neighbors.
        /// </summary>
        /// <param name="origin">The node in question.</param>
        /// <returns>All adjacent neighbors the node has.</returns>
        private List<PotentialNode> GetAdjacentNeighborNodes(PotentialNode origin)
        {
            // Creates a new list that will be populated
            var neighborList = new List<PotentialNode>();

            // Cycles through adjacent neighbors on the x-axis
            for (var x = -1; x <= 1; x++)
            {
                // Skips if x is selecting the origin node
                if (x == 0)
                    continue;

                // Determines the new x-coordinate for the potential neighbor
                var newX = origin.GridX + x;

                // Uses the grid coordinates to create a key for the dictionary
                var gridKey = new Vector2(newX, origin.GridY);

                // Adds the node to the list if it exists on the grid
                if (_gridIndexes.ContainsKey(gridKey))
                    neighborList.Add(_grid[_gridIndexes[gridKey]]);
            }

            // Cycles through the adjacent neighbors on the y-axis
            for (var y = -1; y <= 1; y++)
            {
                // Skips if y is selecting the origin node
                if (y == 0)
                    continue;

                // Determines the new y-coordinate for the potential neighbor
                var newY = origin.GridY + y;

                // Uses the grid coordinates to create a key for the dictionary
                var gridKey = new Vector2(newY, origin.GridX);

                // Adds the node to the list if it exists on the grid
                if (_gridIndexes.ContainsKey(gridKey))
                    neighborList.Add(_grid[_gridIndexes[gridKey]]);
            }

            // Returns the completed neighbor list to the calling function
            return neighborList;
        }

        /// <summary>
        /// Scans a node to retrieve all corner neighbors.
        /// </summary>
        /// <param name="origin">The node in question.</param>
        /// <returns>All corner neighbors the node has.</returns>
        private List<PotentialNode> GetCornerNeighborNodes(PotentialNode origin)
        {
            // Creates a new list that will be populated
            var neighborList = new List<PotentialNode>();

            // Cycles through the grid on the x-axis
            for (var x = -1; x <= 1; x++)
            {
                // Cycles through the grid on the y-axis
                for (var y = -1; y <= 1; y++)
                {
                    // Continues if the selected node is not in a corner position
                    if (x == 0 || y == 0)
                        continue;

                    // Determines the new coordinates for a potential neighbor
                    var newX = origin.GridX + x;
                    var newY = origin.GridY + y;

                    // Uses the grid coordinates to create a key for the dictionary
                    var gridKey = new Vector2(newX, newY);

                    // Adds the node to the list if it exists on the grid
                    if (_gridIndexes.ContainsKey(gridKey))
                        neighborList.Add(_grid[_gridIndexes[gridKey]]);
                }
            }

            // Returns the completed list to the calling function
            return neighborList;
        }

        /// <summary>
        /// Handles removing an agent trigger from the grid.
        /// </summary>
        /// <param name="agentNode">The agent trigger to remove.</param>
        private void RemoveAgentTrigger(AgentTrigger agentNode)
        {
            // Utilizes the PerimeterPool to return the agent trigger to the pool
            PerimeterPool.Instance.RemoveAgentTrigger(agentNode);

            // Removes the agent trigger from the list
            _agentNodes.Remove(agentNode);
        }


        /// <summary>
        /// Retrieves a random node from the grid to test investigations.
        /// </summary>
        /// <returns>The position of a random node that has not been investigated.</returns>
        public Vector3 GetRandomLocation()
        {
            // Selects a random node from the grid
            var randomNode = _grid[Random.Range(0, _grid.Count)];

            // If the node has already been searched, recall this method to retrieve a new one
            if (randomNode.Searched)
                return GetRandomLocation();

            // Returns the position of the selected node to the calling function
            return randomNode.WorldPosition;
        }

#endregion


#region ILLUSTRATION

        /// <summary>
        /// Handles refreshing the illustrated area of nodes for displaying active and inactive areas.
        /// </summary>
        private void RefreshIllustratedArea()
        {
            // Exits if the potential area should not be drawn
            if (!ShowPotentialArea) return;

            // Starts the coroutine to cycle through everything and refresh the area
            StartCoroutine(nameof(RefreshPotentialArea));
        }

        /// <summary>
        /// Handles refreshing the displayed grid area by searching through the various lists.
        /// </summary>
        /// <returns></returns>
        private IEnumerator RefreshPotentialArea()
        {
            // Calls the method to clear out the active nodes
            ClearDrawNodes();

            // Cycles through each node in the grid
            foreach (var n in _grid)
            {
                // Skips the node if it has been searched already
                if (n.Searched)
                    continue;

                // Skips the node if it has been buried
                if (_buriedNodes.Contains(n))
                    continue;

                // Creates a handler to determine if the selected node is active
                var activeNode = false;

                // Cycles through all lists in the inside list
                foreach (var l in _insideExpansionQueue.ExpansionQueue)
                {
                    // Cycles through all nodes in the selected list
                    foreach (var i in l.Nodes)
                    {
                        // Checks if the selected node matches the node found in the inside list
                        if (n == i)
                        {
                            // Informs the hanlder the node is active and leaves the loop
                            activeNode = true;
                            break;
                        }

                        // If the node has been found to be active, it exits the loop
                        if (activeNode)
                            break;
                    }

                    // Exits the loop is the selected node was found to tbe active
                    if (activeNode)
                        break;
                }

                // Checks if theere are any lists within the outside list if the selected node has not been identified
                if (!activeNode)
                {
                    // Cycles through each list in the outside list
                    foreach (var l in _outsideExpansionQueue.ExpansionQueue)
                    {
                        // Cycles through each node in the selected list
                        foreach (var o in l.Nodes)
                        {
                            // If the selected node matches the node found in the outside list
                            if (n == o)
                            {
                                // Informs the handler the node is active and leaves the loop
                                activeNode = true;
                                break;
                            }

                            // If the node has been found to be active, it exits the loop
                            if (activeNode)
                                break;
                        }

                        // Exits the loop if the selected node was found to be active
                        if (activeNode)
                            break;
                    }
                }

                // Calls the method to create a new active node if it is active
                if (activeNode)
                    CreateNewActiveNode(n);

                // Calls the method to create a new inactive node if it is inactive
                else
                    CreateNewInactiveNode(n);
            }

            // Exits the coroutine
            yield return null;
        }

        /// <summary>
        /// Creates a new active node for illustration.
        /// </summary>
        /// <param name="node">The node to illustrate.</param>
        private void CreateNewActiveNode(PotentialNode node)
        {
            // Calls the PerimeterPool to create a new active node at the node's position
            var a = PerimeterPool.Instance.SpawnActiveObject(node.WorldPosition, Vector3.one * _nodeDiameter);

            // Adds the node to the list of active nodes
            _activeObjects.Add(a);
        }

        /// <summary>
        /// Creates a new inactive node for illustration.
        /// </summary>
        /// <param name="node">The node to illustrate then bury.</param>
        private void CreateNewInactiveNode(PotentialNode node)
        {
            // Calls the PerimeterPool to create a new inactive node at the node's position
            var i = PerimeterPool.Instance.SpawnInactiveObject(node.WorldPosition, Vector3.one * _nodeDiameter);

            // Adds the node to the list of inactive nodes then buries it so it does not have to be created again
            _inactiveObjects.Add(i);
            _buriedNodes.Add(node);
        }

        /// <summary>
        /// Clears the active nodes to refresh the illustration of the perimeter.
        /// </summary>
        private void ClearDrawNodes()
        {
            // Cycles through each active node and returns them to the pool
            foreach (var a in _activeObjects)
                PerimeterPool.Instance.RemoveActiveObject(a);

            // Reinitializes the list of active nodes
            _activeObjects = new List<GameObject>();
        }

        /// <summary>
        /// Handles removing the illustration for a node that has been removed.
        /// </summary>
        /// <param name="removedNode">The node that has been removed from the grid.</param>
        private void RemoveIllustration(PotentialNode removedNode)
        {
            // Exits if the potential area is not being drawn
            if (!ShowPotentialArea)
                return;

            // Cycles through the list of inactive node objects
            foreach (var o in _inactiveObjects)
            {
                // If the selected node matches the position of the object
                if (o.transform.position == removedNode.WorldPosition)
                {
                    // Calls the PerimeterPool to return the object to its pool
                    PerimeterPool.Instance.RemoveInactiveObject(o);

                    _inactiveObjects.Remove(o);

                    if (_buriedNodes.Contains(removedNode))
                        _buriedNodes.Remove(removedNode);

                    // Exits the function after the illustration was successfully removed
                    return;
                }
            }

            // Cycles through the list of active node objects
            foreach (var o in _activeObjects)
            {
                // If the selected node matches the position of the object
                if (o.transform.position == removedNode.WorldPosition)
                {
                    // Calls the PerimeterPool to return the object to its pool
                    PerimeterPool.Instance.RemoveActiveObject(o);

                    // Removes the object from the list
                    _activeObjects.Remove(o);

                    // Exits the function after the illustration was successfully removed
                    return;
                }
            }
        }


        /// <summary>
        /// Draws the Gizmos for displaying the perimeter system in the Editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Exits if the checkbox is not selected in the inspector for drawing
            if (!ShowPotentialArea)
                return;

            // Exits if the grid is not initialized or empty
            if (_grid == null || _grid.Count <= 0)
                return;

            // Assigns the color of the Gizmos for displaying all active nodes
            Gizmos.color = ActiveColor;

            // Cycles through the full list to draw Gizmos for each node
            foreach (var n in _grid)
            {
                // Skips the node if it has been investigated
                if (n.Searched)
                    continue;

                // Draws a different Gizmo color at the perimeter origin
                if (n.GridX == 0 && n.GridY == 0)
                {
                    // Assigns the origin color, creates the Gizmo, then sets the color back to active
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(n.WorldPosition, Vector3.one * _nodeDiameter);
                    Gizmos.color = ActiveColor;
                }

                // Draws a wire cube Gizmo for the selected node
                Gizmos.DrawWireCube(n.WorldPosition, Vector3.one * _nodeDiameter);
            }
        }

#endregion

    }

#if UNITY_EDITOR

    /// <summary>
    /// Alters the displayed variables in the inspector depending on the 'ShowPotentialArea' checkbox.
    /// </summary>
    [CustomEditor(typeof(PotentialArea))]
    public class PotentialAreaEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Assigns a handler to the script to handle the inspector GUI
            var script = target as PotentialArea;

            // Exits if the checkbox is not selected
            if (!script.ShowPotentialArea) return;

            // Activates the color fields in the inspector
            script.ActiveColor = EditorGUILayout.ColorField("Active Node Color", script.ActiveColor);
            script.InactiveColor = EditorGUILayout.ColorField("Inactive Node Color", script.InactiveColor);
        }
    }

#endif

}