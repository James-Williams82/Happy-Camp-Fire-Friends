using System;
using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>
    /// N01 — the shift clock: which night it is, how far into the shift, and so which phase and what
    /// time on the camp clock. One clock is shared by every player; only the authority advances it,
    /// and everyone else syncs to it. Story, radio and incidents react to <see cref="PhaseChanged"/>.
    /// </summary>
    public sealed class ShiftClock
    {
        private readonly ShiftSchedule _schedule;

        public ShiftClock(ShiftSchedule schedule, int night = 1, float elapsed = 0f)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            Night = Math.Max(1, night);
            Elapsed = Mathf.Clamp(elapsed, 0f, _schedule.ShiftSeconds);
            PhaseIndex = _schedule.IndexAt(Elapsed, out _);
        }

        public ShiftSchedule Schedule => _schedule;

        /// <summary>The shift number, from 1.</summary>
        public int Night { get; private set; }

        /// <summary>Real seconds into this night's shift.</summary>
        public float Elapsed { get; private set; }

        public int PhaseIndex { get; private set; }

        public ShiftPhase Phase => PhaseIndex >= 0 ? _schedule[PhaseIndex].Phase : ShiftPhase.Prep;

        /// <summary>0–1 through the current phase.</summary>
        public float PhaseProgress
        {
            get
            {
                _schedule.IndexAt(Elapsed, out float progress);
                return progress;
            }
        }

        /// <summary>Real seconds until the next phase.</summary>
        public float PhaseRemaining => PhaseIndex >= 0 ? Mathf.Max(0f, _schedule.StartOf(PhaseIndex + 1) - Elapsed) : 0f;

        /// <summary>The camp clock, 0–24.</summary>
        public float Hours => PhaseIndex >= 0 ? _schedule[PhaseIndex].HourAt(PhaseProgress) : 12f;

        /// <summary>The phase after this one (Prep after Morning).</summary>
        public ShiftPhase NextPhase => _schedule.Count > 0 ? _schedule[(PhaseIndex + 1) % _schedule.Count].Phase : ShiftPhase.Prep;

        /// <summary>The phase changed: (previous, current). Raised once per phase passed, in order. A new night starts at Prep.</summary>
        public event Action<ShiftPhase, ShiftPhase> PhaseChanged;

        /// <summary>Runs the clock forward. Authority only.</summary>
        public void Advance(float seconds)
        {
            if (seconds <= 0f || _schedule.ShiftSeconds <= 0f) return;

            float remaining = seconds;
            while (remaining > 0f)
            {
                float toNext = _schedule.StartOf(PhaseIndex + 1) - Elapsed;
                if (remaining < toNext)
                {
                    Elapsed += remaining;
                    return;
                }

                remaining -= toNext;
                EnterNextPhase();
            }
        }

        /// <summary>Jumps to the start of the next phase. Authority only; for testing and for story beats that end a phase early.</summary>
        public void SkipPhase()
        {
            if (_schedule.Count == 0) return;
            EnterNextPhase();
        }

        /// <summary>Takes the authority's state, e.g. on a client. A jump raises one change, straight to the new phase.</summary>
        public void Sync(int night, float elapsed)
        {
            var previous = Phase;
            int previousIndex = PhaseIndex;
            int previousNight = Night;

            Night = Math.Max(1, night);
            Elapsed = Mathf.Clamp(elapsed, 0f, _schedule.ShiftSeconds);
            PhaseIndex = _schedule.IndexAt(Elapsed, out _);

            if (PhaseIndex != previousIndex || Night != previousNight) PhaseChanged?.Invoke(previous, Phase);
        }

        private void EnterNextPhase()
        {
            var previous = Phase;
            PhaseIndex++;
            if (PhaseIndex >= _schedule.Count)
            {
                PhaseIndex = 0;
                Night++;
            }

            // Zero-length phases are passed through rather than shown, but still announced.
            Elapsed = _schedule.StartOf(PhaseIndex);
            PhaseChanged?.Invoke(previous, Phase);
        }
    }
}
