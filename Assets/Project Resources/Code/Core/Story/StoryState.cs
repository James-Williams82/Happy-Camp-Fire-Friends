using System;

namespace CampfireFriends.Story
{
    /// <summary>Night 1's facts (design doc "Key flags"). Shared, host-owned, never un-raised.</summary>
    [Flags]
    public enum Night01Flag : long
    {
        None = 0,
        ArrivedAtCamp = 1L << 0,       // the ferry tied up at the camp dock
        DockReached = 1L << 1,         // someone stepped off onto the dock
        NoticeInspected = 1L << 2,
        GateOpened = 1L << 3,
        OfficeVisited = 1L << 4,
        FlashlightCollected = 1L << 5,
        MapInspected = 1L << 6,
        WorkOrderViewed = 1L << 7,
        RadioCheckIn = 1L << 8,
        ShedVisited = 1L << 9,
        FuseCollected = 1L << 10,
        BucketFell = 1L << 11,
        FuseInserted = 1L << 12,
        GeneratorFueled = 1L << 13,
        GeneratorStarted = 1L << 14,
        SecurityCircuitOn = 1L << 15,
        RecCircuitOn = 1L << 16,       // the wrong breaker: court lights, harmless
        Cabin06Arrived = 1L << 17,
        Cabin06FirstBoard = 1L << 18,
        Cabin06ExtraCover = 1L << 19,
        Cabin06Secured = 1L << 20,
        DiningHallInspected = 1L << 21,
        PerimeterInspected = 1L << 22,
        EntrySignReported = 1L << 23,
        LeisureNoticed = 1L << 24,     // basketball, fishing
        CabinKnocksPlayed = 1L << 25,
        KnocksReported = 1L << 26,
        CabinImpactPlayed = 1L << 27,
        BackAtOffice = 1L << 28,
        ReportStructural = 1L << 29,
        ReportTrespass = 1L << 30,
        ReportUnknown = 1L << 31,
        Completed = 1L << 32,
        FuelCollected = 1L << 33,
    }

    /// <summary>The story's raised flags. Raising is idempotent; a sync announces only what's new.</summary>
    public sealed class StoryState
    {
        public long Flags { get; private set; }

        /// <summary>A flag went up. <c>live</c> is false when catching up on join, so reactions skip their sounds.</summary>
        public event Action<Night01Flag, bool> Raised;

        public bool Has(Night01Flag flag) => (Flags & (long)flag) == (long)flag;

        /// <summary>True when the flag is new.</summary>
        public bool Raise(Night01Flag flag)
        {
            if (flag == Night01Flag.None || Has(flag)) return false;
            Flags |= (long)flag;
            Raised?.Invoke(flag, true);
            return true;
        }

        public void Sync(long flags, bool live)
        {
            long added = flags & ~Flags;
            Flags |= flags;
            for (int bit = 0; bit < 64 && added != 0; bit++)
            {
                long f = 1L << bit;
                if ((added & f) == 0) continue;
                added &= ~f;
                Raised?.Invoke((Night01Flag)f, live);
            }
        }
    }
}
