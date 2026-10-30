using System;
using System.Collections.Generic;

namespace CampfireFriends.Story
{
    /// <summary>Night 1's seven sequences (design doc "Thirty-minute mission progression").</summary>
    public enum Night01Sequence
    {
        Arrival,     // 1. the dock, the gate, "Welcome to Paradise"
        Office,      // 2. flashlight, map, work orders
        Generator,   // 3. fuse, fuel, starter, SECURITY breaker
        Cabin6,      // 4. board up the opening
        Perimeter,   // 5. dusk; dining-hall latch, perimeter check
        Finale,      // 6. three knocks, the impact
        ShiftLog,    // 7. back at the office, file the report
        Complete,
    }

    /// <summary>What the director needs the world to do that isn't a flag.</summary>
    public enum Night01Request
    {
        /// <summary>Bring the clock on to dusk if it's still day.</summary>
        Dusk,

        /// <summary>Bring the clock on to the night watch if it isn't yet.</summary>
        Night,
    }

    /// <summary>
    /// Runs Night 1 on the authority: listens to the story's flags, moves through the sequences,
    /// and decides what the owner says on the radio and when. It never touches the world itself;
    /// it raises flags (which the world reacts to), speaks lines and makes requests. Event-driven
    /// throughout: the only timers are a stalled-player hint, the gap between related lines, and
    /// the grace before the cabin impact. No path softlocks on player count.
    /// </summary>
    public sealed class Night01Director
    {
        // Seconds without a fuse before the owner's one hint (doc: 80–110 s).
        public const float FuseHintAfter = 95f;

        // Grace between the knocks and the impact, so the group can turn, look and approach.
        public const float ImpactGrace = 9f;

        // Lingering at the cabin after the impact before the owner insists.
        public const float LingerWarning = 25f;

        private readonly StoryState _state;
        private readonly HashSet<int> _spoken = new HashSet<int>();
        private readonly List<(float at, int line)> _queue = new List<(float, int)>();
        private readonly List<(float at, Action action)> _timers = new List<(float, Action)>();
        private float _now;
        private float _radioFreeAt;

        private readonly Func<int, float> _lineSeconds;

        /// <param name="lineSeconds">How long each line takes (a recording's length); reading pace when omitted.</param>
        public Night01Director(StoryState state, Func<int, float> lineSeconds = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _lineSeconds = lineSeconds ?? Night01Lines.ReadingSeconds;
            _state.Raised += OnRaised;
        }

        public Night01Sequence Sequence { get; private set; } = Night01Sequence.Arrival;

        /// <summary>A line goes out on everyone's walkie.</summary>
        public event Action<int> Said;

        public event Action<Night01Request> Requested;

        public event Action<Night01Sequence> SequenceChanged;

        public void Tick(float deltaTime)
        {
            _now += Math.Max(0f, deltaTime);

            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].at > _now) continue;
                var action = _timers[i].action;
                _timers.RemoveAt(i--);
                action();
            }

            // Lines go out one at a time, each after the previous has had time to be read.
            if (_queue.Count > 0 && _queue[0].at <= _now && _radioFreeAt <= _now)
            {
                int line = _queue[0].line;
                _queue.RemoveAt(0);
                _radioFreeAt = _now + _lineSeconds(line);
                Said?.Invoke(line);
            }
        }

        private void OnRaised(Night01Flag flag, bool live)
        {
            switch (flag)
            {
                // Bixby gives the welcome in person on the ferry; the radio is silent until
                // someone picks up a walkie in the office.
                case Night01Flag.OfficeVisited: Enter(Night01Sequence.Office); break;
                case Night01Flag.RadioCheckIn:
                    Enter(Night01Sequence.Office);
                    Say(5); Say(6);
                    break;
                case Night01Flag.FlashlightCollected:
                case Night01Flag.WorkOrderViewed:
                    if (_state.Has(Night01Flag.FlashlightCollected) && _state.Has(Night01Flag.WorkOrderViewed)) Enter(Night01Sequence.Generator);
                    break;

                case Night01Flag.ShedVisited:
                    Enter(Night01Sequence.Generator);
                    Say(9);
                    After(FuseHintAfter, () => { if (!_state.Has(Night01Flag.FuseCollected)) Say(10); });
                    break;
                case Night01Flag.GeneratorStarted: Say(12); break;
                case Night01Flag.SecurityCircuitOn:
                    Say(13);
                    Enter(Night01Sequence.Cabin6);
                    break;
                case Night01Flag.RecCircuitOn: Say(20); break;

                case Night01Flag.Cabin06Arrived:
                    Enter(Night01Sequence.Cabin6);
                    Say(14); Say(15, after: 4f);
                    break;
                case Night01Flag.Cabin06FirstBoard: Say(16); break;
                case Night01Flag.Cabin06ExtraCover: Say(18); break;
                case Night01Flag.Cabin06Secured:
                    Say(17);
                    Enter(Night01Sequence.Perimeter);
                    Requested?.Invoke(Night01Request.Dusk);
                    Say(19, after: 8f);
                    break;

                case Night01Flag.LeisureNoticed: Say(21); break;
                case Night01Flag.EntrySignReported: Say(22); break;
                case Night01Flag.DiningHallInspected:
                case Night01Flag.PerimeterInspected:
                    if (_state.Has(Night01Flag.DiningHallInspected) && _state.Has(Night01Flag.PerimeterInspected))
                    {
                        Enter(Night01Sequence.Finale);
                        Requested?.Invoke(Night01Request.Night);
                    }
                    break;

                case Night01Flag.CabinKnocksPlayed:
                    Enter(Night01Sequence.Finale);
                    After(ImpactGrace, () => _state.Raise(Night01Flag.CabinImpactPlayed));
                    break;
                case Night01Flag.KnocksReported:
                    Say(24); Say(25, after: 2f);
                    if (_state.Has(Night01Flag.Cabin06Secured)) Say(26, after: 3f);
                    break;
                case Night01Flag.CabinImpactPlayed:
                    Say(27);
                    After(LingerWarning, () => { if (!_state.Has(Night01Flag.BackAtOffice)) Say(28); });
                    break;

                case Night01Flag.BackAtOffice:
                    if (!_state.Has(Night01Flag.CabinImpactPlayed)) break;
                    Enter(Night01Sequence.ShiftLog);
                    Say(29);
                    break;
                case Night01Flag.ReportStructural: Close(30); break;
                case Night01Flag.ReportTrespass: Close(31); break;
                case Night01Flag.ReportUnknown: Close(32); break;
            }
        }

        /// <summary>The report is filed: the owner answers it, signs off, and Night 1 is done.</summary>
        private void Close(int answer)
        {
            if (Sequence != Night01Sequence.ShiftLog) return;
            Say(answer);
            Say(33, after: 2f);
            Enter(Night01Sequence.Complete);
            _state.Raise(Night01Flag.Completed);
        }

        /// <summary>Moves forward only: a late flag never sends the night back a sequence.</summary>
        private void Enter(Night01Sequence sequence)
        {
            if (sequence <= Sequence) return;
            Sequence = sequence;
            SequenceChanged?.Invoke(sequence);
        }

        /// <summary>Queues a line once per night. Lines never interrupt each other.</summary>
        private void Say(int line, float after = 0f)
        {
            if (!_spoken.Add(line)) return;

            // A crew moving fast mustn't hear quips minutes late: once lines back up, waiting
            // flavour lines are dropped and only instructions and story beats keep their place.
            if (_queue.Count >= 2) _queue.RemoveAll(q => Flavour.Contains(q.line));

            _queue.Add((_now + after, line));
            _queue.Sort((a, b) => a.at.CompareTo(b.at));
        }

        // Banter that's fine to lose; everything else is an instruction or a beat of the story.
        private static readonly HashSet<int> Flavour = new HashSet<int> { 3, 4, 8, 11, 12, 16, 18, 20, 21, 22, 23 };

        private void After(float seconds, Action action) => _timers.Add((_now + seconds, action));
    }
}
