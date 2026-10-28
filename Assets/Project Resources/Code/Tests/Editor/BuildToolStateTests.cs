using CampfireFriends.Building;
using NUnit.Framework;

namespace CampfireFriends.Tests
{
    public class BuildToolStateTests
    {
        private static BuildToolState Building(int pieces = 3)
        {
            var state = new BuildToolState(pieces);
            state.ToggleBuild();
            return state;
        }

        [Test]
        public void StartsOff()
        {
            Assert.AreEqual(BuildMode.None, new BuildToolState(3).Mode);
        }

        [Test]
        public void ToggleBuild_EntersWithNothingSelected()
        {
            var state = Building();

            Assert.AreEqual(BuildMode.Browse, state.Mode);
            Assert.AreEqual(BuildToolState.NoSelection, state.SelectedIndex);
        }

        [Test]
        public void ToggleBuild_FromAnyMode_TurnsOffAndForgetsSelection()
        {
            var state = Building();
            state.Select(2);

            state.ToggleBuild();
            Assert.AreEqual(BuildMode.None, state.Mode);

            state.ToggleBuild();
            Assert.AreEqual(BuildMode.Browse, state.Mode, "Re-entering never resumes a piece.");
        }

        [Test]
        public void Off_IgnoresEverything()
        {
            var state = new BuildToolState(3);

            state.Select(1);
            state.Inspect(5);
            state.Rotate();
            state.Cycle(1);

            Assert.AreEqual(BuildMode.None, state.Mode);
            Assert.AreEqual(GridRotation.R0, state.Rotation);
        }

        [Test]
        public void Select_StartsPlacing_IgnoresBadSlots()
        {
            var state = Building();

            state.Select(2);
            state.Select(7);

            Assert.AreEqual(BuildMode.Place, state.Mode);
            Assert.AreEqual(2, state.SelectedIndex);
        }

        [Test]
        public void Cycle_OnlyWhilePlacing_WrapsBothWays()
        {
            var state = Building();
            state.Cycle(1);
            Assert.AreEqual(BuildMode.Browse, state.Mode, "Cycling can't pick a piece for you.");

            state.Select(0);
            state.Cycle(-1);
            Assert.AreEqual(2, state.SelectedIndex);
            state.Cycle(1);
            Assert.AreEqual(0, state.SelectedIndex);
        }

        [Test]
        public void Rotate_WrapsAfterFourTurns()
        {
            var state = Building();
            state.Select(0);

            for (int i = 0; i < 5; i++) state.Rotate();

            Assert.AreEqual(GridRotation.R90, state.Rotation);
        }

        [Test]
        public void Inspect_OpensModify_ForThatPiece()
        {
            var state = Building();

            state.Inspect(7);

            Assert.AreEqual(BuildMode.Modify, state.Mode);
            Assert.AreEqual(7, state.TargetId);
        }

        [Test]
        public void BeginMove_StartsFromThePiecesRotation_AndRotatesIndependently()
        {
            var state = Building();
            state.Select(0);
            state.Rotate();                // placing at R90
            state.Inspect(7);

            state.BeginMove(GridRotation.R180);
            Assert.AreEqual(BuildMode.Move, state.Mode);
            Assert.AreEqual(GridRotation.R180, state.Rotation);

            state.Rotate();
            Assert.AreEqual(GridRotation.R270, state.Rotation);

            state.Back();
            state.Back();
            state.Select(0);
            Assert.AreEqual(GridRotation.R90, state.Rotation, "Placing keeps its own rotation.");
        }

        [Test]
        public void BeginMove_OnlyFromModify()
        {
            var state = Building();

            state.BeginMove(GridRotation.R0);

            Assert.AreEqual(BuildMode.Browse, state.Mode);
        }

        [Test]
        public void Back_StepsMoveModifyBrowse()
        {
            var state = Building();
            state.Inspect(7);
            state.BeginMove(GridRotation.R0);

            state.Back();
            Assert.AreEqual(BuildMode.Modify, state.Mode);
            Assert.AreEqual(7, state.TargetId);

            state.Back();
            Assert.AreEqual(BuildMode.Browse, state.Mode);
            Assert.AreEqual(BuildWorld.NoPiece, state.TargetId);

            state.Back();
            Assert.AreEqual(BuildMode.Browse, state.Mode, "Back never leaves build mode.");
        }

        [Test]
        public void Back_FromPlace_PutsThePieceAway()
        {
            var state = Building();
            state.Select(1);

            state.Back();

            Assert.AreEqual(BuildMode.Browse, state.Mode);
            Assert.AreEqual(BuildToolState.NoSelection, state.SelectedIndex);
        }

        [Test]
        public void Deselect_AfterAMove_ClosesTheModifyMenu()
        {
            var state = Building();
            state.Inspect(7);
            state.BeginMove(GridRotation.R0);

            state.Deselect();

            Assert.AreEqual(BuildMode.Browse, state.Mode);
        }

        [Test]
        public void EmptyPalette_NeverEntersPlace()
        {
            var state = Building(0);

            state.Select(0);
            state.Cycle(1);

            Assert.AreNotEqual(BuildMode.Place, state.Mode);
        }

        [Test]
        public void Changed_FiresOnlyOnRealChanges()
        {
            var state = Building();
            int changes = 0;
            state.Changed += () => changes++;

            state.Select(1);
            state.Select(1);
            state.Rotate();

            Assert.AreEqual(2, changes);
        }
    }
}
