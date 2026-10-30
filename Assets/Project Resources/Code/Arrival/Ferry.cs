using System;
using UnityEngine;

namespace CampfireFriends.Arrival
{
    /// <summary>Who decides when the ferry leaves, when it isn't this machine alone.</summary>
    public interface IFerryAuthority
    {
        /// <summary>This player may ring the bell (the host).</summary>
        bool MayRing { get; }

        void RequestBell();
    }

    /// <summary>
    /// The little ferry across Ashwood Lake: a moving deck between the far-shore landing and the
    /// camp dock. Offline it runs itself; in a session the host's <see cref="IFerryAuthority"/>
    /// runs it and this follows. Riders are moved by <see cref="FerryRider"/>.
    /// </summary>
    public class Ferry : MonoBehaviour
    {
        [Tooltip("Where the ferry ties up at the far shore (boarding) and at the camp dock.")]
        [SerializeField] private Transform _farShore;
        [SerializeField] private Transform _campDock;

        [SerializeField, Min(5f)] private float _crossingSeconds = 60f;

        [Tooltip("The walkable deck; anyone standing on it rides along.")]
        [SerializeField] private Collider _deck;

        [Tooltip("Where someone who missed the boat appears at the camp dock.")]
        [SerializeField] private Transform _arrivalPoint;

        public FerryRide Ride { get; private set; }
        public IFerryAuthority Authority { get; set; }
        public Transform ArrivalPoint => _arrivalPoint;

        /// <summary>This player may ring the bell: alone, or as the host.</summary>
        public bool MayRing => Authority == null || Authority.MayRing;

        /// <summary>The ride changed state (cast off, arrived, or synced on joining).</summary>
        public event Action<FerryState> StateChanged;

        private void Awake()
        {
            Ride = new FerryRide(_crossingSeconds);
            Place();
        }

        private void Update()
        {
            if (Authority == null) Tick(Time.deltaTime);
            Place();
        }

        /// <summary>The bell on deck: asks whoever decides. Offline, that's this machine.</summary>
        public void RingBell()
        {
            if (Authority != null) Authority.RequestBell();
            else CastOff();
        }

        /// <summary>Leaves the far shore. The authority only.</summary>
        public bool CastOff()
        {
            if (!Ride.RingBell()) return false;
            StateChanged?.Invoke(Ride.State);
            return true;
        }

        /// <summary>Runs the ride forward. The authority only.</summary>
        public void Tick(float seconds)
        {
            var before = Ride.State;
            Ride.Advance(seconds);
            if (Ride.State != before) StateChanged?.Invoke(Ride.State);
        }

        /// <summary>Takes the authority's ride state.</summary>
        public void Sync(FerryState state, float underWay)
        {
            var before = Ride.State;
            Ride.Sync(state, underWay);
            if (Ride.State != before) StateChanged?.Invoke(Ride.State);
        }

        /// <summary>Someone's feet at <paramref name="feet"/> are standing on the deck.</summary>
        public bool Carries(Vector3 feet)
        {
            if (_deck == null) return false;
            var b = _deck.bounds;
            return Over(feet) && feet.y >= b.max.y - 0.4f && feet.y <= b.max.y + 0.6f;
        }

        /// <summary>A point is above (or below) the deck's footprint, at any height.</summary>
        public bool Over(Vector3 point)
        {
            if (_deck == null) return false;
            var b = _deck.bounds;
            return point.x >= b.min.x && point.x <= b.max.x && point.z >= b.min.z && point.z <= b.max.z;
        }

        private void Place()
        {
            if (_farShore == null || _campDock == null) return;
            transform.SetPositionAndRotation(
                Vector3.Lerp(_farShore.position, _campDock.position, Ride.Progress),
                Quaternion.Slerp(_farShore.rotation, _campDock.rotation, Ride.Progress));
        }
    }
}
