using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;

namespace CampfireFriends.Arrival
{
    /// <summary>
    /// Carries the local player with the ferry while they stand on its deck, through the scene's
    /// locomotor (as teleporting and climbing do). Someone who joins after it has left is put
    /// on the camp dock instead of being stranded on the far shore.
    /// </summary>
    public class FerryRider : MonoBehaviour
    {
        [SerializeField] private Ferry _ferry;
        [SerializeField, Interface(typeof(ILocomotionEventHandler))] private Object _locomotor;
        [SerializeField] private Transform _playerOrigin;
        [SerializeField] private Transform _head;

        private ILocomotionEventHandler Locomotor => _locomotor as ILocomotionEventHandler;
        private Vector3 _lastFerry;

        private void OnEnable()
        {
            if (_ferry == null) return;
            _lastFerry = _ferry.transform.position;
            _ferry.StateChanged += OnFerryChanged;
        }

        private void OnDisable()
        {
            if (_ferry != null) _ferry.StateChanged -= OnFerryChanged;
        }

        private void LateUpdate()
        {
            if (_ferry == null || Locomotor == null || _playerOrigin == null || _head == null) return;

            var moved = _ferry.transform.position - _lastFerry;
            _lastFerry = _ferry.transform.position;
            if (moved.sqrMagnitude < 1e-10f) return;

            // Feet: under the head, at the rig's floor.
            var feet = new Vector3(_head.position.x, _playerOrigin.position.y, _head.position.z);
            if (_ferry.Carries(feet))
                Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), moved, LocomotionEvent.TranslationType.Relative));
        }

        /// <summary>Joined a session whose ferry has already gone: start at the camp dock.</summary>
        private void OnFerryChanged(FerryState state)
        {
            if (state == FerryState.Docked || _ferry.ArrivalPoint == null || Locomotor == null) return;

            var feet = new Vector3(_head.position.x, _playerOrigin.position.y, _head.position.z);
            if (_ferry.Carries(feet)) return;

            // Only strand-proofing: if we're nowhere near the camp, we missed the boat.
            if (Vector3.Distance(feet, _ferry.ArrivalPoint.position) > 60f && state == FerryState.Arrived)
                Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), _ferry.ArrivalPoint.position, LocomotionEvent.TranslationType.Absolute));
        }
    }
}
