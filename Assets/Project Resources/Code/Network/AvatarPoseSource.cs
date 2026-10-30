using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>Samples the local player's head and hands for <see cref="AvatarInput"/>. Point it at the camera rig's anchors.</summary>
    public class AvatarPoseSource : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;

        public AvatarInput Sample()
        {
            var input = new AvatarInput
            {
                HeadRotation = Quaternion.identity,
                LeftRotation = Quaternion.identity,
                RightRotation = Quaternion.identity,
            };

            if (_head != null) _head.GetPositionAndRotation(out input.HeadPosition, out input.HeadRotation);
            if (_leftHand != null) _leftHand.GetPositionAndRotation(out input.LeftPosition, out input.LeftRotation);
            if (_rightHand != null) _rightHand.GetPositionAndRotation(out input.RightPosition, out input.RightRotation);
            return input;
        }
    }
}
