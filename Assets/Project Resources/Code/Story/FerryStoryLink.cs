using CampfireFriends.Arrival;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// The ferry tying up at the camp dock starts Night 1, and the first player off the boat
    /// counts as reaching the dock, wherever they step (the dock zone alone could be missed).
    /// </summary>
    public class FerryStoryLink : MonoBehaviour
    {
        [SerializeField] private Ferry _ferry;
        [SerializeField] private Transform _head;

        [Tooltip("How far from the arrival point stepping off still counts as being at the dock.")]
        [SerializeField, Min(1f)] private float _dockReach = 25f;

        private void OnEnable()
        {
            if (_ferry != null) _ferry.StateChanged += OnFerry;
        }

        private void OnDisable()
        {
            if (_ferry != null) _ferry.StateChanged -= OnFerry;
        }

        private void Update()
        {
            if (_ferry == null || _ferry.Ride.State != FerryState.Arrived) return;
            var story = Stories.Current;
            if (story == null) return;

            // Covers the story authority changing after the ferry already arrived.
            if (!story.State.Has(Night01Flag.ArrivedAtCamp))
            {
                story.Raise(Night01Flag.ArrivedAtCamp);
                return;
            }
            if (story.State.Has(Night01Flag.DockReached)) return;

            if (_head == null && Camera.main != null) _head = Camera.main.transform;
            if (_head == null) return;
            if (_ferry.Over(_head.position)) return;
            if (_ferry.ArrivalPoint != null && Vector3.Distance(_head.position, _ferry.ArrivalPoint.position) > _dockReach) return;
            story.Raise(Night01Flag.DockReached);
        }

        private void OnFerry(FerryState state)
        {
            if (state == FerryState.Arrived) Stories.Current?.Raise(Night01Flag.ArrivedAtCamp);
        }
    }
}
