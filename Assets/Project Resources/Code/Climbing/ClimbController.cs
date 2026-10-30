using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;

namespace CampfireFriends.Climbing
{
    /// <summary>
    /// Climbing for the local player. Grip near a <see cref="Climbable"/> to take hold; the body then
    /// moves through the scene's locomotor so the gripping hand stays on its hold. Letting go above
    /// a climbable's top pulls the player over onto whatever is behind it; letting go anywhere else
    /// drops them to the ground. Other players see it through the avatar, which follows the head
    /// and hands wherever they are.
    /// </summary>
    public class ClimbController : MonoBehaviour
    {
        // Climbing over the top: the head must be within MantleReach of the top edge, and there
        // must be somewhere to stand at most MantleDrop below it.
        private const float MantleReach = 0.4f;
        private const float MantleDrop = 1.2f;

        [SerializeField] private ClimbInputSource _input;

        [Tooltip("The scene's locomotor (PlayerLocomotor): climbing moves the player through it, like teleporting does.")]
        [SerializeField, Interface(typeof(ILocomotionEventHandler))] private Object _locomotor;

        [Tooltip("The rig's floor-level origin, which the locomotor moves.")]
        [SerializeField] private Transform _playerOrigin;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;

        [Header("Feel")]
        [Tooltip("How close (metres) a hand must be to a climbable to grab it.")]
        [SerializeField, Range(0.02f, 0.3f)] private float _reach = 0.1f;

        [Tooltip("What can be climbed, stood on and fallen onto.")]
        [SerializeField] private LayerMask _layers = ~0;

        [Tooltip("How far past the top edge the player lands when climbing over it.")]
        [SerializeField, Range(0.2f, 1f)] private float _mantleStep = 0.65f;

        [SerializeField, Range(0f, 1f)] private float _gripHaptic = 0.35f;

        private ILocomotionEventHandler Locomotor => _locomotor as ILocomotionEventHandler;

        private readonly ClimbState _climb = new ClimbState();
        private readonly Climbable[] _held = new Climbable[2];
        private readonly bool[] _wasGripping = new bool[2];
        private readonly Collider[] _overlaps = new Collider[16];
        private Fall _fall;
        private bool _falling;

        public bool IsClimbing => _climb.IsClimbing;

        /// <summary>Hanging on or dropping off a climb: other movement should leave the body alone.</summary>
        public bool IsBusy => _climb.IsClimbing || _falling;

        private void Update()
        {
            if (_input == null || Locomotor == null || _playerOrigin == null) return;

            for (int i = 0; i < 2; i++)
            {
                var side = (HandSide)i;
                bool gripping = _input.IsGripping(side);
                if (gripping && !_wasGripping[i]) TryGrip(side);
                else if (!gripping && _wasGripping[i]) Let(side);
                _wasGripping[i] = gripping;
            }

            if (_climb.IsClimbing)
            {
                var delta = _climb.BodyDelta(_leftHand.position, _rightHand.position);
                if (delta.sqrMagnitude > 1e-8f) Move(delta);
            }
            else if (_falling)
            {
                FallStep();
            }
        }

        private void TryGrip(HandSide side)
        {
            var hand = Hand(side);
            int count = Physics.OverlapSphereNonAlloc(hand.position, _reach, _overlaps, _layers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var climbable = _overlaps[i].GetComponentInParent<Climbable>();
                if (climbable == null) continue;

                _held[(int)side] = climbable;
                _climb.Grip(side, hand.position);
                _falling = false;
                _fall = default;
                Buzz(side);
                return;
            }
        }

        private void Let(HandSide side)
        {
            var climbable = _held[(int)side];
            _held[(int)side] = null;
            if (!_climb.IsGripping(side)) return;

            if (!_climb.Release(side))
            {
                // Still hanging from the other hand: it holds from where it is now.
                var other = _climb.Leading;
                _climb.Regrip(other, Hand(other).position);
                return;
            }

            if (climbable != null && climbable.MantleAtTop && TryMantle(climbable, side)) return;
            _falling = true;
        }

        /// <summary>Released at the top: step over onto whatever is beyond the edge, if there's ground there.</summary>
        private bool TryMantle(Climbable climbable, HandSide side)
        {
            float top = climbable.Top;
            if (_head.position.y < top - MantleReach) return false;

            var over = Vector3.ProjectOnPlane(Hand(side).position - _head.position, Vector3.up);
            if (over.sqrMagnitude < 0.0001f) over = Vector3.ProjectOnPlane(_head.forward, Vector3.up);
            var target = new Vector3(Hand(side).position.x, top, Hand(side).position.z) + over.normalized * _mantleStep;

            // Only something to step onto near the top counts: a deck, a roof, a platform. Not the
            // ground on the far side of a free-standing ladder.
            if (!Physics.Raycast(target + Vector3.up * 0.5f, Vector3.down, out var hit, 0.5f + MantleDrop, _layers, QueryTriggerInteraction.Ignore))
                return false;

            // Teleport-style move: the feet go to the landing point.
            Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), hit.point, LocomotionEvent.TranslationType.Absolute));
            return true;
        }

        private void FallStep()
        {
            var feet = _playerOrigin.position;
            float gap = Physics.Raycast(_head.position, Vector3.down, out var hit, 200f, _layers, QueryTriggerInteraction.Ignore)
                ? feet.y - hit.point.y
                : 0f;

            float drop = _fall.Step(Time.deltaTime, Mathf.Max(0f, gap));
            if (drop > 0f) Move(Vector3.down * drop);
            if (_fall.Speed <= 0f && drop <= 0f) _falling = false;
        }

        private void Move(Vector3 delta) =>
            Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), delta, LocomotionEvent.TranslationType.Relative));

        private Transform Hand(HandSide side) => side == HandSide.Left ? _leftHand : _rightHand;

        private void Buzz(HandSide side)
        {
            if (_gripHaptic <= 0f) return;
            var controller = side == HandSide.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            OVRInput.SetControllerVibration(1f, _gripHaptic, controller);
            CancelInvoke(side == HandSide.Left ? nameof(StopLeft) : nameof(StopRight));
            Invoke(side == HandSide.Left ? nameof(StopLeft) : nameof(StopRight), 0.06f);
        }

        private void StopLeft() => OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        private void StopRight() => OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}
