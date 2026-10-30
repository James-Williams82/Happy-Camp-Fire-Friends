using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Bixby's recorded lines for Night 1, by line. Filled by the build kit from files named
    /// vo_night01_&lt;doc label&gt; (vo_night01_01 … vo_night01_29, _30a, _30b, _30c, _31). A line
    /// without a recording stays text-only on the walkie.
    /// </summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Radio Voice", fileName = "Night01Voice")]
    public class RadioVoice : ScriptableObject
    {
        [Tooltip("Index = line id (see Night01Lines). Empty slots are text-only.")]
        public AudioClip[] Lines = new AudioClip[34];

        public AudioClip Clip(int id) => id >= 0 && id < Lines.Length ? Lines[id] : null;

        /// <summary>How long the radio is busy with a line: the recording plus a breath, never shorter than reading it.</summary>
        public float Seconds(int id)
        {
            var clip = Clip(id);
            float reading = Night01Lines.ReadingSeconds(id);
            return clip != null ? Mathf.Max(clip.length + 0.6f, Mathf.Min(reading, clip.length + 2f)) : reading;
        }
    }
}
