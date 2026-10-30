using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>Somewhere the shift clock runs: offline on this device, or on the session host.</summary>
    public interface IShiftClockSource
    {
        /// <summary>The clock as this player sees it. Read it; don't advance it.</summary>
        ShiftClock Clock { get; }

        ShiftSettings Settings { get; }

        /// <summary>Paused by the story (Night 1 keeps the afternoon until the work's done). Only the authority's counts.</summary>
        bool Held { get; set; }

        /// <summary>Asks the authority to move on to the next phase (for testing).</summary>
        void RequestSkip();
    }

    /// <summary>The clocks in the scene; the most recently started one (the session's, once connected) is the one in charge.</summary>
    public static class ShiftClocks
    {
        private static readonly List<IShiftClockSource> Active = new List<IShiftClockSource>();

        public static IShiftClockSource Current => Active.Count > 0 ? Active[Active.Count - 1] : null;

        public static void Register(IShiftClockSource source)
        {
            if (source != null && !Active.Contains(source)) Active.Add(source);
        }

        public static void Unregister(IShiftClockSource source) => Active.Remove(source);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReloadFreePlayMode() => Active.Clear();
    }
}
