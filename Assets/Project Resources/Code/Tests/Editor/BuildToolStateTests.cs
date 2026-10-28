using CampfireFriends.Building;
using NUnit.Framework;

namespace CampfireFriends.Tests
{
    public class BuildToolStateTests
    {
        [Test]
        public void StartsIdle()
        {
            var state = new BuildToolState(3);

            Assert.AreEqual(BuildMode.None, state.Mode);
        }

        [Test]
        public void Select_StartsPlacing_IgnoresBadSlots()
        {
            var state = new BuildToolState(3);

            state.Select(2);
            state.Select(7);

            Assert.AreEqual(BuildMode.Place, state.Mode);
            Assert.AreEqual(2, state.SelectedIndex);
        }

        [Test]
        public void Cycle_Wraps_BothWays()
        {
            var state = new BuildToolState(3);
            state.Select(0);

            state.Cycle(-1);
            Assert.AreEqual(2, state.SelectedIndex);

            state.Cycle(1);
            Assert.AreEqual(0, state.SelectedIndex);
        }

        [Test]
        public void Cycle_FromIdle_ResumesTheLastPiece()
        {
            var state = new BuildToolState(3);
            state.Select(1);
            state.Cancel();

            state.Cycle(1);

            Assert.AreEqual(BuildMode.Place, state.Mode);
            Assert.AreEqual(1, state.SelectedIndex);
        }

        [Test]
        public void Rotate_WrapsAfterFourTurns()
        {
            var state = new BuildToolState(1);

            for (int i = 0; i < 5; i++) state.Rotate();

            Assert.AreEqual(GridRotation.R90, state.Rotation);
        }

        [Test]
        public void ToggleDelete_SwitchesBetweenPlaceAndDelete()
        {
            var state = new BuildToolState(1);

            state.ToggleDelete();
            Assert.AreEqual(BuildMode.Place, state.Mode);
            state.ToggleDelete();
            Assert.AreEqual(BuildMode.Delete, state.Mode);
            state.ToggleDelete();
            Assert.AreEqual(BuildMode.Place, state.Mode);
        }

        [Test]
        public void CycleMode_GoesIdlePlaceDeleteIdle()
        {
            var state = new BuildToolState(1);

            state.CycleMode();
            Assert.AreEqual(BuildMode.Place, state.Mode);
            state.CycleMode();
            Assert.AreEqual(BuildMode.Delete, state.Mode);
            state.CycleMode();
            Assert.AreEqual(BuildMode.None, state.Mode);
        }

        [Test]
        public void EmptyPalette_NeverEntersPlace()
        {
            var state = new BuildToolState(0);

            state.CycleMode();
            state.Cycle(1);

            Assert.AreNotEqual(BuildMode.Place, state.Mode);
        }

        [Test]
        public void Changed_FiresOnlyOnRealChanges()
        {
            var state = new BuildToolState(2);
            int changes = 0;
            state.Changed += () => changes++;

            state.Select(1);
            state.Select(1);

            Assert.AreEqual(1, changes);
        }
    }
}
