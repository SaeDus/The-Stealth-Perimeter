namespace The_Stealth_Perimeter.StealthPerimeter
{
    using UnityEngine;

    public class AgentTrigger : MonoBehaviour
    {
        private LayerMask _agentMask;

        public PotentialNode AssignedNode => _assignedNode;
        private PotentialNode _assignedNode;

        private bool _initialized;


        private void OnDisable()
        {
            _initialized = false;
        }


        public void InitializeTrigger(PotentialNode assignedNode, LayerMask agentMask)
        {
            _assignedNode = assignedNode;
            _agentMask = agentMask;
            _initialized = true;
        }


        private void OnTriggerEnter(Collider other)
        {
            if (!_initialized)
                return;

            if ((_agentMask.value & (1 << other.gameObject.layer)) > 0)
                NodeInvestigated();
        }


        private void NodeInvestigated()
        {
            PotentialArea.Instance.NodeInvestigated(this);

            gameObject.SetActive(false);
        }
    }
}