using System;
using UnityEngine;

namespace CampfireFriends.Arrival
{
    /// <summary>One recorded line and the beat that follows it.</summary>
    [Serializable]
    public struct SpokenLine
    {
        public AudioClip Clip;
        [Min(0f)] public float PauseAfter;

        public float Seconds => (Clip != null ? Clip.length : 0f) + PauseAfter;
    }
}
