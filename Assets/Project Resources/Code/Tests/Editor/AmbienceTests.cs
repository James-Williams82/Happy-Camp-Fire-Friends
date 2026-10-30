using CampfireFriends.Ambience;
using NUnit.Framework;

namespace CampfireFriends.Tests
{
    /// <summary>The atmospheric audio's timing: layered, irregular, and quiet when out of place.</summary>
    public class AmbienceTests
    {
        private static int Count(ScatterTimer timer, float seconds, float presence, float step = 0.1f)
        {
            int count = 0;
            for (float t = 0f; t < seconds; t += step) if (timer.Tick(step, presence)) count++;
            return count;
        }

        [Test]
        public void Scatter_PlaysWithinItsIntervals()
        {
            var timer = new ScatterTimer(10f, 20f, seed: 1);

            int count = Count(timer, 600f, presence: 1f);

            Assert.That(count, Is.InRange(30, 60), "600 s at one sound every 10–20 s.");
        }

        [Test]
        public void Scatter_IsSilent_OutOfItsTimeOfDay()
        {
            var timer = new ScatterTimer(1f, 2f, seed: 1);

            Assert.AreEqual(0, Count(timer, 600f, presence: 0f));
        }

        [Test]
        public void Scatter_ThinsOut_AsPresenceFalls()
        {
            int full = Count(new ScatterTimer(5f, 10f, seed: 7), 600f, presence: 1f);
            int faint = Count(new ScatterTimer(5f, 10f, seed: 7), 600f, presence: 0.25f);

            Assert.Less(faint, full / 2);
            Assert.Greater(faint, 0);
        }

        [Test]
        public void Gusts_StayInRange_AndVary()
        {
            var wind = new GustModel(seed: 3);
            float min = 1f, max = 0f;
            for (float t = 0f; t < 300f; t += 0.5f)
            {
                float s = wind.Strength(t, gustiness: 1f);
                Assert.That(s, Is.InRange(0f, 1f));
                min = System.Math.Min(min, s);
                max = System.Math.Max(max, s);
            }

            Assert.Greater(max - min, 0.25f, "The wind should swell and drop, not drone.");
        }

        [Test]
        public void Gusts_AreRepeatable_ForTheSameSeed()
        {
            Assert.AreEqual(new GustModel(5).Strength(42f, 0.7f), new GustModel(5).Strength(42f, 0.7f));
        }

        [Test]
        public void Gusts_AreCalmer_WithLowGustiness()
        {
            var wind = new GustModel(seed: 9);
            float gusty = 0f, calm = 0f;
            for (float t = 0f; t < 300f; t += 0.5f)
            {
                gusty = System.Math.Max(gusty, wind.Strength(t, 1f));
                calm = System.Math.Max(calm, wind.Strength(t, 0f));
            }

            Assert.Less(calm, gusty);
        }
    }
}
