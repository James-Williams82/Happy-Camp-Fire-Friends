using System.Collections.Generic;
using CampfireFriends.Shifts;
using NUnit.Framework;

namespace CampfireFriends.Tests
{
    /// <summary>Design doc §6 — the shift state machine (N01): PREP → DUSK → NIGHT_WATCH → MORNING → PREP.</summary>
    public class ShiftClockTests
    {
        // 10 s per phase keeps the arithmetic readable.
        private static ShiftSchedule Schedule() => new ShiftSchedule(new[]
        {
            new PhaseSpan(ShiftPhase.Prep, 10f, 15f, 19f),
            new PhaseSpan(ShiftPhase.Dusk, 10f, 19f, 21f),
            new PhaseSpan(ShiftPhase.NightWatch, 10f, 21f, 5f),
            new PhaseSpan(ShiftPhase.Morning, 10f, 5f, 8f),
        });

        private static List<(ShiftPhase from, ShiftPhase to)> Record(ShiftClock clock)
        {
            var changes = new List<(ShiftPhase, ShiftPhase)>();
            clock.PhaseChanged += (from, to) => changes.Add((from, to));
            return changes;
        }

        [Test]
        public void DefaultShift_NightLastsTwiceAsLongAsDay()
        {
            var schedule = ShiftSchedule.Condensed();

            Assert.Greater(schedule.LightSeconds, 0f);
            Assert.AreEqual(2f * schedule.LightSeconds, schedule.DarkSeconds, 0.001f);
        }

        [Test]
        public void StartsAtPrepOfNightOne()
        {
            var clock = new ShiftClock(Schedule());

            Assert.AreEqual(1, clock.Night);
            Assert.AreEqual(ShiftPhase.Prep, clock.Phase);
            Assert.AreEqual(15f, clock.Hours, 0.001f);
        }

        [Test]
        public void Hours_RunThroughThePhase()
        {
            var clock = new ShiftClock(Schedule());
            clock.Advance(5f);

            Assert.AreEqual(ShiftPhase.Prep, clock.Phase);
            Assert.AreEqual(17f, clock.Hours, 0.001f, "Halfway through 15:00–19:00.");
            Assert.AreEqual(5f, clock.PhaseRemaining, 0.001f);
        }

        [Test]
        public void Hours_WrapPastMidnight_DuringTheWatch()
        {
            var clock = new ShiftClock(Schedule());
            clock.Advance(25f); // halfway through 21:00 → 05:00

            Assert.AreEqual(ShiftPhase.NightWatch, clock.Phase);
            Assert.AreEqual(1f, clock.Hours, 0.001f);
        }

        [Test]
        public void Advance_AnnouncesEveryPhasePassed_InOrder()
        {
            var clock = new ShiftClock(Schedule());
            var changes = Record(clock);

            clock.Advance(25f);

            CollectionAssert.AreEqual(new[]
            {
                (ShiftPhase.Prep, ShiftPhase.Dusk),
                (ShiftPhase.Dusk, ShiftPhase.NightWatch),
            }, changes);
        }

        [Test]
        public void AfterMorning_TheNextNightBeginsAtPrep()
        {
            var clock = new ShiftClock(Schedule());
            var changes = Record(clock);

            clock.Advance(41f);

            Assert.AreEqual(2, clock.Night);
            Assert.AreEqual(ShiftPhase.Prep, clock.Phase);
            Assert.AreEqual(1f, clock.Elapsed, 0.001f);
            Assert.AreEqual((ShiftPhase.Morning, ShiftPhase.Prep), changes[changes.Count - 1]);
        }

        [Test]
        public void SkipPhase_JumpsToTheStartOfTheNext()
        {
            var clock = new ShiftClock(Schedule());
            clock.Advance(3f);
            var changes = Record(clock);

            clock.SkipPhase();

            Assert.AreEqual(ShiftPhase.Dusk, clock.Phase);
            Assert.AreEqual(19f, clock.Hours, 0.001f);
            Assert.AreEqual(1, changes.Count);
        }

        [Test]
        public void Sync_TakesTheAuthoritysTime_AndAnnouncesOneChange()
        {
            var client = new ShiftClock(Schedule());
            var changes = Record(client);

            client.Sync(1, 25f);

            Assert.AreEqual(ShiftPhase.NightWatch, client.Phase);
            CollectionAssert.AreEqual(new[] { (ShiftPhase.Prep, ShiftPhase.NightWatch) }, changes);
        }

        [Test]
        public void Sync_WithinTheSamePhase_AnnouncesNothing()
        {
            var client = new ShiftClock(Schedule());
            var changes = Record(client);

            client.Sync(1, 4f);
            client.Sync(1, 8f);

            Assert.IsEmpty(changes);
            Assert.AreEqual(8f, client.Elapsed, 0.001f);
        }

        [Test]
        public void AnEmptySchedule_NeverAdvances()
        {
            var clock = new ShiftClock(new ShiftSchedule(new PhaseSpan[0]));
            clock.Advance(100f);
            clock.SkipPhase();

            Assert.AreEqual(1, clock.Night);
            Assert.AreEqual(0f, clock.Elapsed);
        }

        [Test]
        public void ZeroLengthPhases_ArePassedThrough_ButStillAnnounced()
        {
            var clock = new ShiftClock(new ShiftSchedule(new[]
            {
                new PhaseSpan(ShiftPhase.Prep, 10f, 15f, 19f),
                new PhaseSpan(ShiftPhase.Dusk, 0f, 19f, 21f),
                new PhaseSpan(ShiftPhase.NightWatch, 10f, 21f, 5f),
            }));
            var changes = Record(clock);

            clock.Advance(12f);

            Assert.AreEqual(ShiftPhase.NightWatch, clock.Phase);
            Assert.AreEqual(2, changes.Count);
        }

        [Test]
        public void Sun_RisesEast_PeaksSouth_SetsWest_AndIsDownAtMidnight()
        {
            var path = SunPath.Default; // 06:30 → 19:30

            path.Sun(6.5f, out float riseElevation, out float riseAzimuth);
            path.Sun(13f, out float noonElevation, out float noonAzimuth);
            path.Sun(19.5f, out float setElevation, out float setAzimuth);
            path.Sun(1f, out float midnightElevation, out _);

            Assert.AreEqual(0f, riseElevation, 0.01f);
            Assert.AreEqual(90f, riseAzimuth, 0.01f);
            Assert.AreEqual(path.NoonElevation, noonElevation, 0.01f);
            Assert.AreEqual(180f, noonAzimuth, 0.01f);
            Assert.AreEqual(0f, setElevation, 0.01f);
            Assert.AreEqual(270f, setAzimuth, 0.01f);
            Assert.Less(midnightElevation, 0f);
        }

        [Test]
        public void Moon_IsUpOnlyAtNight()
        {
            var path = SunPath.Default;

            path.Moon(1f, out float night, out _);
            path.Moon(13f, out float day, out _);

            Assert.Greater(night, 0f);
            Assert.Less(day, 0f);
            Assert.IsFalse(path.IsDay(1f));
            Assert.IsTrue(path.IsDay(13f));
        }
    }
}
