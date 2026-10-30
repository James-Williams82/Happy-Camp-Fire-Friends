using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Small sounds generated at startup where the project has no recording: a knuckle knock on a
    /// wooden door, a footstep on dirt, one on boards, and the walkie's squelch.
    /// </summary>
    public static class StorySounds
    {
        private const int Rate = 32000;

        /// <summary>A hollow wooden knock: a sharp click into a short low resonance.</summary>
        public static AudioClip Knock(int seed) => Make("Knock", seed, 0.35f, (t, r) =>
        {
            float body = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 28f);
            float tone = Mathf.Sin(2f * Mathf.PI * 410f * t) * Mathf.Exp(-t * 55f) * 0.5f;
            float click = (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 400f) * 0.6f;
            return (body + tone + click) * 0.9f;
        });

        /// <summary>A soft footfall on earth.</summary>
        public static AudioClip Step(int seed) => Make("Step", seed, 0.25f, Thud(90f, 22f, 0.35f));

        /// <summary>A footfall on a wooden walkway: duller, with a creak of the board.</summary>
        public static AudioClip WoodStep(int seed) => Make("Wood Step", seed, 0.4f, (t, r) =>
            Thud(120f, 16f, 0.25f)(t, r) + Mathf.Sin(2f * Mathf.PI * (300f + 60f * t) * t) * Mathf.Exp(-t * 9f) * 0.12f);

        /// <summary>The walkie keying up: a burst of squelch.</summary>
        public static AudioClip Squelch(int seed) => Make("Squelch", seed, 0.22f, (t, r) =>
            (float)(r.NextDouble() * 2 - 1) * 0.25f * (t < 0.18f ? 1f : Mathf.Exp(-(t - 0.18f) * 60f)) * (0.6f + 0.4f * Mathf.Sin(t * 900f)));

        private static System.Func<float, System.Random, float> Thud(float pitch, float decay, float noise) => (t, r) =>
            Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Exp(-t * decay) + (float)(r.NextDouble() * 2 - 1) * noise * Mathf.Exp(-t * decay * 1.6f);

        private static AudioClip Make(string name, int seed, float seconds, System.Func<float, System.Random, float> sample)
        {
            var random = new System.Random(seed);
            var data = new float[(int)(seconds * Rate)];
            float last = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                // A gentle low-pass keeps the generated edge off.
                last += (sample(i / (float)Rate, random) - last) * 0.5f;
                data[i] = Mathf.Clamp(last, -1f, 1f);
            }
            var clip = AudioClip.Create($"{name} (generated)", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
