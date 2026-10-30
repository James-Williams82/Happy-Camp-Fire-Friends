using CampfireFriends.Shifts;
using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// The session's one shift clock (N01). The host runs it, starting from wherever its offline
    /// clock had got to; everyone else follows the night and time it shares, so the whole team goes
    /// through dusk and the watch together. Anyone may ask the host to skip a phase (testing).
    /// </summary>
    public class NetworkShiftClock : NetworkBehaviour, IShiftClockSource
    {
        [SerializeField] private ShiftSettings _settings;

        [Networked] private int Night { get; set; }
        [Networked] private float Elapsed { get; set; }

        public ShiftClock Clock { get; private set; }
        public ShiftSettings Settings => _settings;
        public bool Held { get; set; }

        public override void Spawned()
        {
            Clock = new ShiftClock(_settings != null ? _settings.CreateSchedule() : ShiftSchedule.Condensed());

            if (HasStateAuthority)
            {
                var offline = ShiftClocks.Current;
                if (offline != null && offline.Clock != null) Clock.Sync(offline.Clock.Night, offline.Clock.Elapsed);
                Share();
            }
            else
            {
                Clock.Sync(Night, Elapsed);
            }

            ShiftClocks.Register(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => ShiftClocks.Unregister(this);

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (!Held) Clock.Advance(Runner.DeltaTime);
            Share();
        }

        public override void Render()
        {
            if (!HasStateAuthority) Clock.Sync(Night, Elapsed);
        }

        public void RequestSkip() => RPC_RequestSkip();

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestSkip()
        {
            Clock.SkipPhase();
            Share();
        }

        private void Share()
        {
            Night = Clock.Night;
            Elapsed = Clock.Elapsed;
        }
    }
}
