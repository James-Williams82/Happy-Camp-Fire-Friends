using UnityEngine;

namespace CampfireFriends.Climbing
{
    /// <summary>
    /// Grip to climb: the controller's grip button, or an index pinch with tracked hands. A little
    /// hysteresis keeps a half-squeezed grip from flickering.
    /// </summary>
    public class OvrClimbInput : ClimbInputSource
    {
        [SerializeField, Range(0.1f, 1f)] private float _gripOn = 0.6f;
        [SerializeField, Range(0f, 0.9f)] private float _gripOff = 0.35f;

        [Tooltip("Tracked hands, for pinch-to-grip when not holding controllers. Optional.")]
        [SerializeField] private OVRHand _leftHand;
        [SerializeField] private OVRHand _rightHand;

        private readonly bool[] _held = new bool[2];

        public override bool IsGripping(HandSide side)
        {
            int i = (int)side;
            var controller = side == HandSide.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

            if (OVRInput.IsControllerConnected(controller))
            {
                float grip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
                _held[i] = grip >= (_held[i] ? _gripOff : _gripOn);
                return _held[i];
            }

            var hand = side == HandSide.Left ? _leftHand : _rightHand;
            _held[i] = hand != null && hand.IsTracked && hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            return _held[i];
        }
    }
}
