using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>One tick of a player's tracked pose: head and both hands, in world space.</summary>
    public struct AvatarInput : INetworkInput
    {
        public Vector3 HeadPosition;
        public Quaternion HeadRotation;
        public Vector3 LeftPosition;
        public Quaternion LeftRotation;
        public Vector3 RightPosition;
        public Quaternion RightRotation;
    }
}
