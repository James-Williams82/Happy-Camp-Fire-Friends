using System;
using System.Collections.Generic;
using CampfireFriends.Story;
using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// Night 1 in a session. The host runs the director (starting from the offline story's flags)
    /// and shares the flags, the sequence and every line spoken; guests mirror them and send what
    /// they find to the host. A late joiner gets the whole state and the radio log, without the
    /// sounds of things that already happened.
    /// </summary>
    public class NetworkStory : NetworkBehaviour, IStoryAuthority
    {
        [Tooltip("Bixby's recordings; their lengths pace the radio. Optional.")]
        [SerializeField] private RadioVoice _voice;

        [Networked] private long Flags { get; set; }
        [Networked] private byte SharedSequence { get; set; }
        [Networked] private int LineCount { get; set; }
        [Networked, Capacity(48)] private NetworkArray<byte> Lines => default;

        private readonly List<int> _log = new List<int>();
        private Night01Director _director;

        public StoryState State { get; } = new StoryState();
        public Night01Sequence Sequence => _director != null ? _director.Sequence : (Night01Sequence)SharedSequence;
        public IReadOnlyList<int> Log => _log;

        public event Action<int, bool> LineSpoken;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                var offline = Stories.Current;
                if (offline != null) State.Sync(offline.State.Flags, live: false);

                _director = new Night01Director(State, _voice != null ? _voice.Seconds : (Func<int, float>)null);
                _director.Said += OnSaid;
                _director.Requested += Stories.Apply;
                _director.SequenceChanged += s => SharedSequence = (byte)s;
                State.Raised += (flag, live) => Flags = State.Flags;
                Flags = State.Flags;
            }
            else
            {
                CatchUp(live: false);
            }

            Stories.Register(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState) => Stories.Unregister(this);

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            _director.Tick(Runner.DeltaTime);
            Stories.HoldDaylight(_director.Sequence);
        }

        public override void Render()
        {
            if (!HasStateAuthority) CatchUp(live: true);
        }

        public void Raise(Night01Flag flag)
        {
            if (!Stories.Running) return;
            if (HasStateAuthority) State.Raise(flag);
            else if (!State.Has(flag)) RPC_Raise((long)flag);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_Raise(long flag) => State.Raise((Night01Flag)flag);

        private void OnSaid(int line)
        {
            _log.Add(line);
            if (LineCount < Lines.Length) Lines.Set(LineCount, (byte)line);
            LineCount++;
            LineSpoken?.Invoke(line, true);
        }

        private void CatchUp(bool live)
        {
            State.Sync(Flags, live);
            int count = Mathf.Min(LineCount, Lines.Length);
            while (_log.Count < count)
            {
                int line = Lines[_log.Count];
                _log.Add(line);
                LineSpoken?.Invoke(line, live);
            }
        }
    }
}
