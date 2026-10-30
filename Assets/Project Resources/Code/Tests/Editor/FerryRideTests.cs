using CampfireFriends.Arrival;
using NUnit.Framework;

namespace CampfireFriends.Tests
{
    public class FerryRideTests
    {
        [Test]
        public void WaitsForTheBell_ThenCrossesAndStaysArrived()
        {
            var ferry = new FerryRide(60f);
            ferry.Advance(100f);
            Assert.AreEqual(FerryState.Docked, ferry.State, "It doesn't leave by itself.");

            Assert.IsTrue(ferry.RingBell());
            ferry.Advance(30f);
            Assert.AreEqual(FerryState.Crossing, ferry.State);
            Assert.AreEqual(0.5f, ferry.Progress, 0.0001f);

            ferry.Advance(31f);
            Assert.AreEqual(FerryState.Arrived, ferry.State);
            Assert.AreEqual(1f, ferry.Progress);
            Assert.IsFalse(ferry.RingBell(), "The bell does nothing once it has left.");
        }

        [Test]
        public void StartsAndStopsGently()
        {
            var ferry = new FerryRide(60f);
            ferry.RingBell();
            ferry.Advance(3f);

            Assert.Less(ferry.Progress, 3f / 60f, "Slower than full speed while pulling away.");
        }
    }
}
