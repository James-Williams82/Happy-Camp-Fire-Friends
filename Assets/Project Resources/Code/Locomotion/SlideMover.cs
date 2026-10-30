using System;
using CampfireFriends.Climbing;
using CampfireFriends.Construction;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CampfireFriends.Locomotion
{
    /// <summary>
    /// Walking with the left stick instead of teleporting. The body slides where the head faces,
    /// steps up kerbs and onto docks, strides over small gaps (dock to boat), drops down off
    /// edges, and stops at walls and at anything marked not to stand on (the lake). Holding the
    /// left trigger sprints while there's <see cref="Stamina"/>; A jumps. Moves go through the
    /// scene's locomotor, like climbing and riding the ferry, so they all add up.
    /// </summary>
    public class SlideMover : MonoBehaviour
    {
        [Tooltip("The scene's locomotor (PlayerLocomotor).")]
        [SerializeField, Interface(typeof(ILocomotionEventHandler))] private Object _locomotor;

        [Tooltip("The rig's floor-level origin, which the locomotor moves.")]
        [SerializeField] private Transform _playerOrigin;
        [SerializeField] private Transform _head;

        [Tooltip("Optional: sliding pauses while hanging on a climb.")]
        [SerializeField] private ClimbController _climbing;

        [Tooltip("Optional: no jumping while building (A rotates pieces there).")]
        [SerializeField] private BuildController _building;

        [SerializeField] private OVRInput.Controller _stick = OVRInput.Controller.LTouch;

        [Header("Feel")]
        [SerializeField, Range(0.5f, 4f)] private float _speed = 2f;
        [SerializeField, Range(1f, 3f)] private float _sprintFactor = 1.9f;
        [SerializeField, Range(0f, 0.5f)] private float _deadZone = 0.15f;
        [Tooltip("The highest step (metres) walked straight up onto.")]
        [SerializeField, Range(0.1f, 0.6f)] private float _stepUp = 0.45f;
        [Tooltip("The widest gap (metres) strode across, like dock to boat.")]
        [SerializeField, Range(0f, 1.2f)] private float _stride = 0.8f;
        [Tooltip("The body's half-width when bumping into walls.")]
        [SerializeField, Range(0.1f, 0.5f)] private float _radius = 0.25f;
        [SerializeField, Range(1f, 20f)] private float _gravity = 9.8f;
        [SerializeField, Range(0f, 6f)] private float _jumpSpeed = 3.4f;

        [Header("Sprint")]
        [SerializeField, Min(1f)] private float _sprintSeconds = 6f;
        [SerializeField, Min(1f)] private float _refillSeconds = 8f;

        [Tooltip("What can be stood on and walked into.")]
        [SerializeField] private LayerMask _layers = ~0;

        /// <summary>Sprint left, for the meter.</summary>
        public Stamina Stamina { get; private set; }

        public bool Grounded { get; private set; } = true;
        public bool Sprinting { get; private set; }

        /// <summary>Walked this far along the ground this frame (for footsteps); true when sprinting.</summary>
        public event Action<float, bool> Walked;

        private ILocomotionEventHandler Locomotor => _locomotor as ILocomotionEventHandler;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private float _verticalSpeed;

        private void Awake() => Stamina = new Stamina(_sprintSeconds, _refillSeconds);

        private void Update()
        {
            if (Locomotor == null || _playerOrigin == null || _head == null) return;
            float dt = Time.deltaTime;
            if (_climbing != null && _climbing.IsBusy)
            {
                _verticalSpeed = 0f;
                Sprinting = Stamina.Tick(false, dt);
                return;
            }

            var feet = new Vector3(_head.position.x, _playerOrigin.position.y, _head.position.z);
            var move = Vector3.zero;

            var axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _stick);
            bool moving = axis.magnitude > _deadZone;
            bool sprintHeld = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _stick) > 0.5f;
            Sprinting = Stamina.Tick(moving && sprintHeld, dt);

            if (moving)
            {
                var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(_head.right, Vector3.up).normalized;
                float pace = Mathf.InverseLerp(_deadZone, 1f, Mathf.Min(1f, axis.magnitude)) * _speed * (Sprinting ? _sprintFactor : 1f);
                move = WallAware(feet, (forward * axis.y + right * axis.x).normalized * (pace * dt));

                // Off the edge of anything not to be stood on (the lake): stay put, unless it's
                // only a stride across to somewhere that can be.
                if (move != Vector3.zero && Ground(feet + move, out _, out bool standable) && !standable && !StrideAcross(feet, move))
                    move = Vector3.zero;
            }

            if (Grounded && _jumpSpeed > 0f && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)
                && (_building == null || !_building.State.IsBuilding))
            {
                _verticalSpeed = _jumpSpeed;
                Grounded = false;
            }

            // Follow the ground: up steps at once, down by falling; a jump rises then falls.
            float rise;
            bool hasGround = Ground(feet + move, out float groundY, out bool solid);
            float gap = hasGround ? groundY - feet.y : float.NegativeInfinity;
            if (_verticalSpeed <= 0f && (gap > -0.02f || !solid))
            {
                // Mid-stride over the water, keep level rather than sinking into it.
                rise = solid ? gap : 0f;
                _verticalSpeed = 0f;
                Grounded = true;
            }
            else
            {
                _verticalSpeed -= _gravity * dt;
                rise = _verticalSpeed * dt;
                Grounded = false;
                if (hasGround && _verticalSpeed <= 0f && feet.y + rise <= groundY)
                {
                    rise = gap;
                    _verticalSpeed = 0f;
                    Grounded = true;
                }
                else if (!hasGround) rise = 0f;
            }

            var delta = move + Vector3.up * rise;
            if (delta.sqrMagnitude > 1e-10f)
                Locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), delta, LocomotionEvent.TranslationType.Relative));
            if (Grounded && move != Vector3.zero) Walked?.Invoke(move.magnitude, Sprinting);
        }

        /// <summary>Standable ground within a stride ahead, no higher than a step.</summary>
        private bool StrideAcross(Vector3 feet, Vector3 move)
        {
            if (_stride <= 0f) return false;
            var ahead = move.normalized;
            for (float d = 0.2f; d <= _stride + 1e-4f; d += 0.2f)
            {
                if (Ground(feet + ahead * d, out float y, out bool standable) && standable && y - feet.y <= _stepUp && y - feet.y > -_stepUp)
                    return true;
            }
            return false;
        }

        /// <summary>The move, cut short or turned along whatever wall is in the way.</summary>
        private Vector3 WallAware(Vector3 feet, Vector3 move)
        {
            for (int attempt = 0; attempt < 2 && move.sqrMagnitude > 1e-10f; attempt++)
            {
                if (!Wall(feet, move, out var normal)) return move;
                move = Vector3.ProjectOnPlane(move, new Vector3(normal.x, 0f, normal.z).normalized);
            }
            return Wall(feet, move, out _) ? Vector3.zero : move;
        }

        private bool Wall(Vector3 feet, Vector3 move, out Vector3 normal)
        {
            normal = Vector3.zero;
            float distance = move.magnitude;
            if (distance < 1e-5f) return false;

            // A body from just above step height to the chest.
            var bottom = feet + Vector3.up * (_stepUp + _radius);
            var top = feet + Vector3.up * Mathf.Max(_stepUp + _radius, 1.4f);
            int count = Physics.CapsuleCastNonAlloc(bottom, top, _radius, move / distance, _hits, distance + 0.05f, _layers, QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (Mine(hit.collider) || hit.distance <= 0f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                normal = hit.normal;
            }
            return nearest < float.MaxValue;
        }

        /// <summary>The floor under a point, found from step height above the feet down.</summary>
        private bool Ground(Vector3 feet, out float y, out bool standable)
        {
            y = 0f;
            standable = true;
            int count = Physics.RaycastNonAlloc(feet + Vector3.up * _stepUp, Vector3.down, _hits, 200f, _layers, QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            Collider floor = null;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (Mine(hit.collider) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                y = hit.point.y;
                floor = hit.collider;
            }
            if (floor == null) return false;

            var surface = floor.GetComponentInParent<TeleportInteractable>();
            standable = surface == null || surface.AllowTeleport;
            return true;
        }

        private bool Mine(Collider c) => c.transform.IsChildOf(_playerOrigin);
    }
}
