using CampfireFriends.Supplies;
using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// A scene supply source (R01) such as a lumber stack or scrap bin. The host owns the remaining
    /// quantity; any player can ask to take from it, and what they take goes into the shared stock.
    /// </summary>
    public class SupplyPoint : NetworkBehaviour
    {
        [Tooltip("Durable id for saves and story triggers. Defaults to the object name.")]
        [SerializeField] private string _nodeId;
        [SerializeField] private ResourceType _type = ResourceType.Boards;
        [SerializeField] private int _quantity = 10;
        [SerializeField] private int _perCollect = 1;

        [Networked] public int Remaining { get; private set; }

        private SupplyNode _node;

        public ResourceType Type => _type;
        public bool IsDepleted => Remaining <= 0;

        public override void Spawned()
        {
            if (!HasStateAuthority) return;

            _node = new SupplyNode(string.IsNullOrEmpty(_nodeId) ? name : _nodeId, _type, _quantity);
            Remaining = _node.Remaining;
        }

        /// <summary>Called by the local interaction (grab, button, debug panel).</summary>
        public void RequestCollect() => RPC_RequestCollect(_perCollect);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestCollect(int amount)
        {
            var stock = ConstructionAuthority.Instance != null ? ConstructionAuthority.Instance.HostStock : null;
            if (stock == null) return;

            _node.Collect(Mathf.Clamp(amount, 0, _perCollect), stock);
            Remaining = _node.Remaining;
        }
    }
}
