using System;
using System.Collections.Generic;
using CampfireFriends.Game;
using CampfireFriends.Shifts;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Where Night 1's story lives: offline on this machine, or on the session host. Everyone
    /// reads the same flags and hears the same lines; anyone may report a fact with <see cref="Raise"/>.
    /// </summary>
    public interface IStoryAuthority
    {
        StoryState State { get; }
        Night01Sequence Sequence { get; }

        /// <summary>Lines spoken so far tonight, oldest first (the walkie's log).</summary>
        IReadOnlyList<int> Log { get; }

        /// <summary>A line went out. <c>live</c> is false when catching up after joining.</summary>
        event Action<int, bool> LineSpoken;

        /// <summary>Reports a fact; the authority decides. Ignored outside Story mode.</summary>
        void Raise(Night01Flag flag);
    }

    /// <summary>The story in charge: the session's once connected, otherwise this machine's.</summary>
    public static class Stories
    {
        private static readonly List<IStoryAuthority> Active = new List<IStoryAuthority>();

        public static IStoryAuthority Current => Active.Count > 0 ? Active[Active.Count - 1] : null;

        /// <summary>Raised when <see cref="Current"/> changes, so views rebind.</summary>
        public static event Action Changed;

        public static void Register(IStoryAuthority story)
        {
            if (story == null || Active.Contains(story)) return;
            Active.Add(story);
            Changed?.Invoke();
        }

        public static void Unregister(IStoryAuthority story)
        {
            if (Active.Remove(story)) Changed?.Invoke();
        }

        /// <summary>Only Story mode tells Night 1; Creative is just building.</summary>
        public static bool Running => GameModes.Active == GameMode.Story;

        /// <summary>The director asked for the time of day to move on: dusk for the perimeter walk, night for the finale.</summary>
        public static void Apply(Night01Request request)
        {
            var clock = ShiftClocks.Current;
            if (clock?.Clock == null) return;

            var phase = clock.Clock.Phase;
            bool behind = request == Night01Request.Dusk ? phase == ShiftPhase.Prep : phase == ShiftPhase.Prep || phase == ShiftPhase.Dusk;
            if (!behind) return;

            clock.RequestSkip();
            if (request == Night01Request.Night && phase == ShiftPhase.Prep) clock.RequestSkip();
        }

        // How far into dusk the light settles while the camp is still dark.
        private const float LateDusk = 0.6f;

        /// <summary>
        /// Called by the authority each frame: in Story mode the afternoon lasts until the perimeter
        /// walk, dusk deepens during the ferry ride and then waits for the power to come on, and a
        /// night watch lasts until the shift log is filed (no dawn mid-story).
        /// </summary>
        public static void HoldDaylight(Night01Sequence sequence)
        {
            var clock = ShiftClocks.Current;
            if (clock?.Clock == null) return;
            var phase = clock.Clock.Phase;
            clock.Held = Running && ((sequence < Night01Sequence.Perimeter && phase == ShiftPhase.Prep)
                                     || (sequence < Night01Sequence.Cabin6 && phase == ShiftPhase.Dusk && clock.Clock.PhaseProgress >= LateDusk)
                                     || (sequence < Night01Sequence.Complete && phase == ShiftPhase.NightWatch));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReloadFreePlayMode()
        {
            Active.Clear();
            Changed = null;
        }
    }
}
