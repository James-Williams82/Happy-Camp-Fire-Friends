using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>N01 — the stages of one security shift, in order. After Morning the next night's Prep begins.</summary>
    public enum ShiftPhase
    {
        /// <summary>Briefing and preparation in daylight: collect supplies, repair, fortify.</summary>
        Prep,

        /// <summary>The light goes: check lights, choose patrol routes, first radio check-in.</summary>
        Dusk,

        /// <summary>The watch itself: patrol and respond to disturbances.</summary>
        NightWatch,

        /// <summary>Shift end: compare notes, assess damage, report.</summary>
        Morning,
    }

    /// <summary>
    /// One phase of the shift: how long it lasts in real seconds, and the camp clock hours it spans.
    /// The end hour may be past midnight (21 → 5); hours always run forward.
    /// </summary>
    [Serializable]
    public struct PhaseSpan
    {
        public ShiftPhase Phase;
        [Min(0f)] public float Seconds;
        [Range(0f, 24f)] public float StartHour;
        [Range(0f, 24f)] public float EndHour;

        public PhaseSpan(ShiftPhase phase, float seconds, float startHour, float endHour)
        {
            Phase = phase;
            Seconds = seconds;
            StartHour = startHour;
            EndHour = endHour;
        }

        /// <summary>Camp hours covered, going forward from the start (wrapping past midnight).</summary>
        public float HourSpan => Mathf.Repeat(EndHour - StartHour, 24f);

        /// <summary>The camp clock (0–24) at <paramref name="progress"/> (0–1) through the phase.</summary>
        public float HourAt(float progress) => Mathf.Repeat(StartHour + HourSpan * Mathf.Clamp01(progress), 24f);
    }

    /// <summary>The ordered phases of one shift. Clock time and story progression are separate: this only says what time it is.</summary>
    public sealed class ShiftSchedule
    {
        private readonly PhaseSpan[] _phases;

        public ShiftSchedule(IEnumerable<PhaseSpan> phases)
        {
            _phases = new List<PhaseSpan>(phases ?? Array.Empty<PhaseSpan>()).ToArray();
            foreach (var phase in _phases) ShiftSeconds += Mathf.Max(0f, phase.Seconds);
        }

        /// <summary>
        /// A condensed test shift (30 minutes): Prep 15:00–19:00, Dusk to 21:00, Night watch to 05:00,
        /// Morning to 08:00. Dark lasts twice as long as light: 20 minutes against 10.
        /// </summary>
        public static ShiftSchedule Condensed() => new ShiftSchedule(DefaultPhases());

        public static PhaseSpan[] DefaultPhases() => new[]
        {
            new PhaseSpan(ShiftPhase.Prep, 480f, 15f, 19f),
            new PhaseSpan(ShiftPhase.Dusk, 120f, 19f, 21f),
            new PhaseSpan(ShiftPhase.NightWatch, 1080f, 21f, 5f),
            new PhaseSpan(ShiftPhase.Morning, 120f, 5f, 8f),
        };

        /// <summary>Dusk and the watch are the dark part of a shift; Prep and Morning the light.</summary>
        public static bool IsDark(ShiftPhase phase) => phase == ShiftPhase.Dusk || phase == ShiftPhase.NightWatch;

        /// <summary>Real seconds of the shift spent dark.</summary>
        public float DarkSeconds
        {
            get
            {
                float dark = 0f;
                foreach (var phase in _phases) if (IsDark(phase.Phase)) dark += Mathf.Max(0f, phase.Seconds);
                return dark;
            }
        }

        /// <summary>Real seconds of the shift spent in daylight.</summary>
        public float LightSeconds => ShiftSeconds - DarkSeconds;

        public int Count => _phases.Length;
        public PhaseSpan this[int index] => _phases[index];

        /// <summary>Real seconds in one whole shift.</summary>
        public float ShiftSeconds { get; }

        /// <summary>Seconds into the shift at which phase <paramref name="index"/> begins.</summary>
        public float StartOf(int index)
        {
            float start = 0f;
            for (int i = 0; i < index && i < _phases.Length; i++) start += Mathf.Max(0f, _phases[i].Seconds);
            return start;
        }

        /// <summary>The phase <paramref name="elapsed"/> seconds into the shift falls in, and how far through it.</summary>
        public int IndexAt(float elapsed, out float progress)
        {
            progress = 0f;
            if (_phases.Length == 0) return -1;

            float start = 0f;
            for (int i = 0; i < _phases.Length; i++)
            {
                float seconds = Mathf.Max(0f, _phases[i].Seconds);
                if (elapsed < start + seconds)
                {
                    progress = seconds > 0f ? (elapsed - start) / seconds : 1f;
                    return i;
                }
                start += seconds;
            }

            progress = 1f;
            return _phases.Length - 1;
        }
    }
}
