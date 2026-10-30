using CampfireFriends.Arrival;
using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// Shares the ferry in a session: the host runs it and rings the bell, everyone else follows.
    /// A late joiner gets the ferry where it is now (and is put on the camp dock if it has gone).
    /// </summary>
    public class NetworkFerry : NetworkBehaviour, IFerryAuthority
    {
        [SerializeField] private Ferry _ferry;

        [Networked] private byte State { get; set; }
        [Networked] private float UnderWay { get; set; }

        public bool MayRing => HasStateAuthority;

        public override void Spawned()
        {
            if (_ferry == null) return;
            _ferry.Authority = this;

            if (HasStateAuthority) Share();
            else _ferry.Sync((FerryState)State, UnderWay);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_ferry != null && ReferenceEquals(_ferry.Authority, this)) _ferry.Authority = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _ferry == null) return;
            _ferry.Tick(Runner.DeltaTime);
            Share();
        }

        public override void Render()
        {
            if (!HasStateAuthority && _ferry != null) _ferry.Sync((FerryState)State, UnderWay);
        }

        public void RequestBell() => RPC_RingBell();

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RingBell(RpcInfo info = default)
        {
            // The host's call: guests can't send everyone off early.
            if (!info.IsInvokeLocal || _ferry == null) return;
            _ferry.CastOff();
            Share();
        }

        private void Share()
        {
            State = (byte)_ferry.Ride.State;
            UnderWay = _ferry.Ride.UnderWay;
        }
    }
}
