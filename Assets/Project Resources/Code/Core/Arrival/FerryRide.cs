using UnityEngine;

namespace CampfireFriends.Arrival
{
    public enum FerryState
    {
        /// <summary>At the far shore, boarding. Waiting for the bell.</summary>
        Docked,

        /// <summary>Under way across the lake.</summary>
        Crossing,

        /// <summary>Tied up at the camp dock.</summary>
        Arrived,
    }

    /// <summary>
    /// The ferry that brings a new crew across Ashwood Lake: boards at the far shore, leaves when
    /// the bell is rung, crosses with a gentle start and stop, and stays at the camp dock. Only the
    /// authority advances it; everyone else follows its state and time under way.
    /// </summary>
    public sealed class FerryRide
    {
        public FerryRide(float crossingSeconds)
        {
            CrossingSeconds = Mathf.Max(1f, crossingSeconds);
        }

        public float CrossingSeconds { get; }
        public FerryState State { get; private set; } = FerryState.Docked;

        /// <summary>Seconds since it left the far shore.</summary>
        public float UnderWay { get; private set; }

        /// <summary>0 at the far shore, 1 at the camp dock, eased so it never lurches.</summary>
        public float Progress
        {
            get
            {
                if (State == FerryState.Docked) return 0f;
                if (State == FerryState.Arrived) return 1f;
                float t = Mathf.Clamp01(UnderWay / CrossingSeconds);
                return t * t * (3f - 2f * t);
            }
        }

        /// <summary>Casts off. Only from the far shore; true when it left.</summary>
        public bool RingBell()
        {
            if (State != FerryState.Docked) return false;
            State = FerryState.Crossing;
            UnderWay = 0f;
            return true;
        }

        public void Advance(float seconds)
        {
            if (State != FerryState.Crossing || seconds <= 0f) return;
            UnderWay = Mathf.Min(CrossingSeconds, UnderWay + seconds);
            if (UnderWay >= CrossingSeconds) State = FerryState.Arrived;
        }

        /// <summary>Takes the authority's state, e.g. on a client or a late joiner.</summary>
        public void Sync(FerryState state, float underWay)
        {
            State = state;
            UnderWay = Mathf.Clamp(underWay, 0f, CrossingSeconds);
        }
    }
}
