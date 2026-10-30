using UnityEngine;

namespace CampfireFriends.Ambience
{
    /// <summary>
    /// How hard the wind is blowing over time, 0–1: a slow swell with irregular gusts on top, never
    /// periodic enough to notice. The same seed and time always give the same strength.
    /// </summary>
    public readonly struct GustModel
    {
        private readonly float _offset;

        public GustModel(int seed)
        {
            _offset = (seed & 0xFFFF) * 1.618f;
        }

        /// <param name="time">Seconds.</param>
        /// <param name="gustiness">0 is a steady breeze; 1 is the full gust range.</param>
        public float Strength(float time, float gustiness)
        {
            // Swells over half a minute or so, gusts over a few seconds, flutter on top.
            float swell = Mathf.PerlinNoise(_offset, time * 0.035f);
            float gust = Mathf.PerlinNoise(_offset + 31.7f, time * 0.15f);
            float flutter = Mathf.PerlinNoise(_offset + 77.3f, time * 0.9f);

            // Gusts only count above their mid-point, so calm spells are real.
            float gusts = Mathf.Clamp01((gust - 0.45f) * 2.2f);
            float strength = 0.2f + 0.45f * swell + gustiness * (0.5f * gusts + 0.1f * (flutter - 0.5f));
            return Mathf.Clamp01(strength);
        }
    }
}
