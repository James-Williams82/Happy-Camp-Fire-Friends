using System;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Clips for one action, optionally per piece size. A size with no clips of its own uses
    /// <see cref="All"/>, so one generic sound can cover every size until specific ones exist.
    /// Several clips per slot are variations, picked at random.
    /// </summary>
    [Serializable]
    public class SizedClips
    {
        public AudioClip[] All = new AudioClip[0];
        public AudioClip[] Small = new AudioClip[0];
        public AudioClip[] Medium = new AudioClip[0];
        public AudioClip[] Large = new AudioClip[0];

        public AudioClip Pick(PieceSize size)
        {
            var sized = size == PieceSize.Small ? Small : size == PieceSize.Large ? Large : Medium;
            return BuildSoundSet.PickFrom(sized != null && sized.Length > 0 ? sized : All);
        }
    }

    /// <summary>
    /// Every building sound in one asset. The build kit builder fills it from
    /// Assets/Audio/Building by file name (sfx_build_{action}_{small|medium|large|all}_{nn}),
    /// so adding a variation is dropping in a file and rebuilding.
    /// </summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Building/Build Sound Set", fileName = "BuildSounds")]
    public class BuildSoundSet : ScriptableObject
    {
        [Header("At the piece, heard by everyone")]
        public SizedClips Place = new SizedClips();
        public SizedClips Remove = new SizedClips();

        [Tooltip("A piece turned in place or repositioned.")]
        public SizedClips Move = new SizedClips();

        [Tooltip("A placed piece used with build mode off, e.g. a light switched.")]
        public SizedClips Use = new SizedClips();

        [Header("Local cues")]
        public AudioClip[] Refused = new AudioClip[0];
        public AudioClip[] Select = new AudioClip[0];
        public AudioClip[] MenuPick = new AudioClip[0];
        public AudioClip[] ModeOn = new AudioClip[0];
        public AudioClip[] ModeOff = new AudioClip[0];

        [Header("Variation")]
        [Tooltip("Random pitch spread around 1, so repeated sounds don't feel mechanical.")]
        [Range(0f, 0.3f)] public float PitchJitter = 0.06f;

        public float RandomPitch() => 1f + UnityEngine.Random.Range(-PitchJitter, PitchJitter);

        public static AudioClip PickFrom(AudioClip[] clips) =>
            clips != null && clips.Length > 0 ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;
    }
}
