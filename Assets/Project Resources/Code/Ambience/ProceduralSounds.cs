using UnityEngine;

namespace CampfireFriends.Ambience
{
    /// <summary>
    /// Sounds generated at startup, so the camp has an environment before any recordings exist:
    /// a seamless wind loop, a leaf-rustle loop and a few distant cricket chirps. Low sample rates
    /// keep them small in memory on Quest.
    /// </summary>
    public static class ProceduralSounds
    {
        private const int LoopRate = 22050;
        private const int ChirpRate = 32000;

        /// <summary>Stereo brown noise: the body of the wind. A low-pass filter on its source shapes the gusts.</summary>
        public static AudioClip Wind(int seed, float seconds = 16f)
        {
            var random = new System.Random(seed);
            int frames = (int)(seconds * LoopRate);
            var left = Brown(random, frames + LoopRate);
            var right = Brown(random, frames + LoopRate);

            var data = new float[frames * 2];
            var l = Seamless(left, frames, LoopRate);
            var r = Seamless(right, frames, LoopRate);
            for (int i = 0; i < frames; i++)
            {
                data[i * 2] = l[i];
                data[i * 2 + 1] = r[i];
            }

            var clip = AudioClip.Create("Wind (generated)", frames, 2, LoopRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Mono hiss with an uneven flutter: leaves moving. Its source's volume follows the gusts.</summary>
        public static AudioClip Rustle(int seed, float seconds = 12f)
        {
            var random = new System.Random(seed);
            int frames = (int)(seconds * LoopRate);
            int total = frames + LoopRate;
            var raw = new float[total];

            float previous = 0f, envelope = 0f, target = 0f;
            for (int i = 0; i < total; i++)
            {
                // A first difference of white noise is high-passed: hiss without rumble.
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                float hiss = white - previous;
                previous = white;

                // Every few milliseconds the leaves pick a new loudness; the envelope glides to it.
                if (i % 160 == 0) target = Mathf.Pow((float)random.NextDouble(), 2.5f);
                envelope += (target - envelope) * 0.004f;
                raw[i] = hiss * envelope * 0.5f;
            }

            var clip = AudioClip.Create("Rustle (generated)", frames, 1, LoopRate, false);
            clip.SetData(Seamless(raw, frames, LoopRate), 0);
            return clip;
        }

        /// <summary>A few short cricket chirps, each a different pitch and number of pulses.</summary>
        public static AudioClip[] CricketChirps(int seed, int count = 4)
        {
            var random = new System.Random(seed);
            var chirps = new AudioClip[count];
            for (int c = 0; c < count; c++)
            {
                float frequency = 4200f + (float)random.NextDouble() * 1000f;
                int pulses = 2 + random.Next(3);
                float pulseSeconds = 0.018f + (float)random.NextDouble() * 0.012f;
                float gapSeconds = 0.016f + (float)random.NextDouble() * 0.01f;

                int pulseFrames = (int)(pulseSeconds * ChirpRate);
                int gapFrames = (int)(gapSeconds * ChirpRate);
                var data = new float[pulses * (pulseFrames + gapFrames) + gapFrames];

                for (int p = 0; p < pulses; p++)
                {
                    int start = p * (pulseFrames + gapFrames);
                    float loudness = 1f - 0.15f * p;
                    for (int i = 0; i < pulseFrames; i++)
                    {
                        float t = (float)i / ChirpRate;
                        float envelope = Mathf.Sin(Mathf.PI * i / pulseFrames);
                        float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.2f * Mathf.Sin(4f * Mathf.PI * frequency * t);
                        data[start + i] = tone * envelope * envelope * loudness * 0.6f;
                    }
                }

                chirps[c] = AudioClip.Create($"Cricket (generated) {c + 1}", data.Length, 1, ChirpRate, false);
                chirps[c].SetData(data, 0);
            }
            return chirps;
        }

        private static float[] Brown(System.Random random, int frames)
        {
            var data = new float[frames];
            float value = 0f;
            for (int i = 0; i < frames; i++)
            {
                value = (value + 0.02f * (float)(random.NextDouble() * 2.0 - 1.0)) / 1.02f;
                data[i] = value * 3.5f;
            }
            return data;
        }

        /// <summary>The first <paramref name="frames"/> samples, with the tail beyond them crossfaded into the start so the loop has no click.</summary>
        private static float[] Seamless(float[] source, int frames, int fade)
        {
            var data = new float[frames];
            for (int i = 0; i < frames; i++) data[i] = source[i];
            for (int i = 0; i < fade && frames + i < source.Length; i++)
            {
                float t = (float)i / fade;
                data[i] = source[i] * t + source[frames + i] * (1f - t);
            }
            return data;
        }
    }
}
