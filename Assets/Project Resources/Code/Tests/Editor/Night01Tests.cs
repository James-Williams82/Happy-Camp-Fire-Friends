using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Story;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Night 1's director and Cabin 6's boarding check (design doc acceptance criteria).</summary>
    public class Night01Tests
    {
        private static (StoryState state, Night01Director director, List<int> said) Start()
        {
            var state = new StoryState();
            var director = new Night01Director(state);
            var said = new List<int>();
            director.Said += said.Add;
            return (state, director, said);
        }

        private static void Run(Night01Director director, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.25f) director.Tick(0.25f);
        }

        [Test]
        public void TheWholeNight_RunsInOrder_AndCompletes()
        {
            var (state, director, said) = Start();
            foreach (var flag in new[]
            {
                Night01Flag.DockReached, Night01Flag.GateOpened, Night01Flag.OfficeVisited,
                Night01Flag.FlashlightCollected, Night01Flag.WorkOrderViewed, Night01Flag.ShedVisited,
                Night01Flag.FuseCollected, Night01Flag.FuseInserted, Night01Flag.GeneratorFueled,
                Night01Flag.GeneratorStarted, Night01Flag.SecurityCircuitOn, Night01Flag.Cabin06Arrived,
                Night01Flag.Cabin06Secured, Night01Flag.DiningHallInspected, Night01Flag.PerimeterInspected,
                Night01Flag.CabinKnocksPlayed,
            })
            {
                state.Raise(flag);
                Run(director, 2f);
            }

            Run(director, Night01Director.ImpactGrace + 1f);
            Assert.IsTrue(state.Has(Night01Flag.CabinImpactPlayed), "The impact follows the knocks.");

            state.Raise(Night01Flag.BackAtOffice);
            Assert.AreEqual(Night01Sequence.ShiftLog, director.Sequence);

            state.Raise(Night01Flag.ReportUnknown);
            Run(director, 200f);

            Assert.AreEqual(Night01Sequence.Complete, director.Sequence);
            Assert.IsTrue(state.Has(Night01Flag.Completed));
            Assert.Contains(32, said, "The answer matches the report.");
            Assert.AreEqual(33, said[said.Count - 1], "Sign-off is the last word.");
        }

        [Test]
        public void LinesNeverOverlap_AndEachPlaysOnce()
        {
            var (state, director, said) = Start();
            state.Raise(Night01Flag.RadioCheckIn); // lines 5 and 6 together
            director.Tick(0.1f);
            Assert.AreEqual(new[] { 5 }, said.ToArray(), "The second waits for the first to be read.");

            Run(director, 30f);
            Assert.AreEqual(new[] { 5, 6 }, said.ToArray());
        }

        [Test]
        public void TheRadio_StaysQuiet_UntilSomeoneTakesAWalkie()
        {
            var (state, director, said) = Start();
            state.Raise(Night01Flag.DockReached);
            state.Raise(Night01Flag.GateOpened);
            state.Raise(Night01Flag.NoticeInspected);
            state.Raise(Night01Flag.OfficeVisited);
            Run(director, 60f);
            CollectionAssert.IsEmpty(said, "Bixby said his piece on the ferry.");

            state.Raise(Night01Flag.RadioCheckIn);
            Run(director, 60f);
            Assert.AreEqual(new[] { 5, 6 }, said.ToArray());
        }

        [Test]
        public void StalledAtTheShed_GetsOneHint_OnlyIfStillStuck()
        {
            var (state, director, said) = Start();
            state.Raise(Night01Flag.ShedVisited);
            Run(director, Night01Director.FuseHintAfter + 20f);
            Assert.Contains(10, said);

            var (state2, director2, said2) = Start();
            state2.Raise(Night01Flag.ShedVisited);
            state2.Raise(Night01Flag.FuseCollected);
            Run(director2, Night01Director.FuseHintAfter + 20f);
            CollectionAssert.DoesNotContain(said2, 10);
        }

        [Test]
        public void ReturningEarly_BeforeTheImpact_DoesNotStartTheShiftLog()
        {
            var (state, director, _) = Start();
            state.Raise(Night01Flag.BackAtOffice);
            state.Raise(Night01Flag.ReportStructural);

            Assert.AreNotEqual(Night01Sequence.Complete, director.Sequence);
            Assert.IsFalse(state.Has(Night01Flag.Completed));
        }

        [Test]
        public void Sync_AnnouncesNewFlagsOnly()
        {
            var state = new StoryState();
            state.Raise(Night01Flag.GateOpened);
            var raised = new List<Night01Flag>();
            state.Raised += (f, live) => raised.Add(f);

            state.Sync((long)(Night01Flag.GateOpened | Night01Flag.OfficeVisited), live: false);

            Assert.AreEqual(new[] { Night01Flag.OfficeVisited }, raised.ToArray());
        }

        [Test]
        public void Doorway_CountsAsSecured_WhenCovered_HoweverItWasBoarded()
        {
            // Cabin 6: door in the east wall at x −3.0, z 26–27, 2 m tall → cells x −6, z 52–53, y 0–3.
            var opening = new OpeningCoverage(new Vector3Int(-6, 0, 52), new Vector3Int(-5, 4, 54), Vector3Int.right);
            var world = new BuildWorld();

            Assert.AreEqual(0f, opening.Covered(world, out _));

            // One wall-sized board just outside the doorway.
            world.Add(new PlacedPiece(1, "wall", new CellBounds(new Vector3Int(-6, 0, 52), new Vector3Int(1, 4, 2)), GridRotation.R90, 0, 100));
            Assert.AreEqual(1f, opening.Covered(world, out int pieces));
            Assert.AreEqual(1, pieces);
        }

        [Test]
        public void Doorway_HalfBoarded_IsHalfCovered()
        {
            var opening = new OpeningCoverage(new Vector3Int(-6, 0, 52), new Vector3Int(-5, 4, 54), Vector3Int.right);
            var world = new BuildWorld();
            // A block one cell further out still counts (boards needn't sit exactly in the plane).
            world.Add(new PlacedPiece(1, "block", new CellBounds(new Vector3Int(-5, 0, 52), new Vector3Int(2, 2, 2)), GridRotation.R0, 0, 100));

            Assert.AreEqual(0.5f, opening.Covered(world, out _), 0.0001f);
        }
    }
}
