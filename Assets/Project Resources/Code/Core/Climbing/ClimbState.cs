using UnityEngine;

namespace CampfireFriends.Climbing
{
    public enum HandSide
    {
        Left,
        Right,
    }

    /// <summary>
    /// Hand-over-hand climbing: each gripping hand is pinned to the spot it grabbed, and the body
    /// moves so the most recent grip stays put. Letting go with one hand hands the climb to the
    /// other; letting go with both ends it.
    /// </summary>
    public sealed class ClimbState
    {
        private readonly Vector3?[] _anchors = new Vector3?[2];
        private HandSide _leading;

        public bool IsClimbing => _anchors[0].HasValue || _anchors[1].HasValue;

        public bool IsGripping(HandSide side) => _anchors[(int)side].HasValue;

        /// <summary>The hand the body hangs from: the one that gripped last.</summary>
        public HandSide Leading => _leading;

        /// <summary>Grips at <paramref name="handPosition"/>; this hand now leads.</summary>
        public void Grip(HandSide side, Vector3 handPosition)
        {
            _anchors[(int)side] = handPosition;
            _leading = side;
        }

        /// <summary>Lets go. True when that was the last hand holding on.</summary>
        public bool Release(HandSide side)
        {
            if (!_anchors[(int)side].HasValue) return false;

            _anchors[(int)side] = null;
            var other = side == HandSide.Left ? HandSide.Right : HandSide.Left;
            if (_anchors[(int)other].HasValue)
            {
                // The other hand takes the weight from where it is now holding.
                _leading = other;
                return false;
            }
            return true;
        }

        /// <summary>Where the leading hand grabbed, if anywhere.</summary>
        public Vector3? Anchor => _anchors[(int)_leading];

        /// <summary>
        /// How far the body must move this frame so the leading hand is back on its hold: the hand
        /// moving down pulls the body up, and so on. Zero when not climbing.
        /// </summary>
        public Vector3 BodyDelta(Vector3 leftHand, Vector3 rightHand)
        {
            var anchor = _anchors[(int)_leading];
            if (!anchor.HasValue) return Vector3.zero;

            var hand = _leading == HandSide.Left ? leftHand : rightHand;
            return anchor.Value - hand;
        }

        /// <summary>
        /// The other hand, which may have drifted while not leading, re-takes its hold where it is
        /// now; call when the lead changes so the body doesn't jump back to an old grip.
        /// </summary>
        public void Regrip(HandSide side, Vector3 handPosition)
        {
            if (_anchors[(int)side].HasValue) _anchors[(int)side] = handPosition;
        }
    }

    /// <summary>
    /// Falling after letting go: speeds up under gravity to a terminal speed and stops exactly on
    /// the ground, never through it.
    /// </summary>
    public struct Fall
    {
        public float Speed { get; private set; }

        /// <summary>How far to drop this frame. <paramref name="groundBelow"/> is the gap under the feet; landing resets the fall.</summary>
        public float Step(float deltaTime, float groundBelow, float gravity = 9.81f, float terminalSpeed = 20f)
        {
            if (groundBelow <= 0.001f)
            {
                Speed = 0f;
                return 0f;
            }

            Speed = Mathf.Min(terminalSpeed, Speed + gravity * deltaTime);
            float drop = Mathf.Min(groundBelow, Speed * deltaTime);
            if (drop >= groundBelow) Speed = 0f;
            return drop;
        }
    }
}
