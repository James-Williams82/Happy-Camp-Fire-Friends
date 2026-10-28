using System.Collections.Generic;
using CampfireFriends.Building;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Geometric acceptance tests GEO-01…GEO-20 from the build-system doc §19.2.</summary>
    public class PlacementSolverTests
    {
        private BuildTestKit _kit;

        [SetUp] public void SetUp() => _kit = new BuildTestKit();
        [TearDown] public void TearDown() => _kit.Dispose();

        private static IEnumerable<TestCaseData> FacesAndRotations()
        {
            var faces = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
            foreach (var face in faces)
            foreach (GridRotation rotation in System.Enum.GetValues(typeof(GridRotation)))
                yield return new TestCaseData(face, rotation).SetName($"Contact_IsFlush_AndSharesFace({face}, {rotation})");
        }

        // GEO-02…GEO-05, GEO-10: every face, every quarter turn — flush, no overlap, no seam.
        [TestCaseSource(nameof(FacesAndRotations))]
        public void Contact_IsFlush_AndSharesFace(Vector3Int face, GridRotation rotation)
        {
            var target = _kit.PlaceOrFail("block", Vector3Int.zero);
            var targetWorld = _kit.Grid.ToWorldBounds(target.Bounds);
            var hit = targetWorld.center + Vector3.Scale(face, targetWorld.extents);

            var proposal = _kit.AimAtPiece("wall", target, hit, face, rotation);

            Assert.IsTrue(proposal.HasPose);
            Assert.AreEqual(rotation.RotateSize(BuildTestKit.WallSize), proposal.Bounds.Size, "Rotation must swap width and depth.");
            Assert.IsFalse(proposal.Bounds.Overlaps(target.Bounds), "Touching faces must not overlap.");

            int axis = face.x != 0 ? 0 : face.y != 0 ? 1 : 2;
            int sign = face[axis];
            if (sign > 0) Assert.AreEqual(target.Bounds.Max[axis], proposal.Bounds.Min[axis], "Candidate min must meet target max.");
            else Assert.AreEqual(target.Bounds.Min[axis], proposal.Bounds.Max[axis], "Candidate max must meet target min.");

            for (int i = 0; i < 3; i++)
            {
                if (i == axis) continue;
                Assert.Less(proposal.Bounds.Min[i], target.Bounds.Max[i], $"Axis {i} must share the face.");
                Assert.Less(target.Bounds.Min[i], proposal.Bounds.Max[i], $"Axis {i} must share the face.");
            }
        }

        [Test] // GEO-01
        public void Ground_CentresOnAim_AndSitsOnLattice()
        {
            var proposal = _kit.AimAtGround("block", new Vector3(1.1f, 0.02f, 1.1f));

            Assert.AreEqual(new Vector3Int(1, 0, 1), proposal.Min);
            Assert.AreEqual(Alignment.Contact, proposal.AlignY);
        }

        [Test]
        public void Ground_SlightlyUneven_RoundsToNearestLevel()
        {
            Assert.AreEqual(0, _kit.AimAtGround("block", new Vector3(0f, 0.2f, 0f)).Min.y);
            Assert.AreEqual(1, _kit.AimAtGround("block", new Vector3(0f, 0.3f, 0f)).Min.y);
        }

        [Test] // GEO-02
        public void WallBesideWall_PlusX_LinesUpEdges()
        {
            var target = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", target, new Vector3(1f, 0.2f, 0.25f), Vector3.right);

            Assert.AreEqual(new Vector3Int(2, 0, 0), proposal.Min);
        }

        [Test] // GEO-03
        public void WallBesideWall_MinusX_LinesUpEdges()
        {
            var target = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", target, new Vector3(0f, 0.2f, 0.25f), Vector3.left);

            Assert.AreEqual(new Vector3Int(-2, 0, 0), proposal.Min);
        }

        [Test] // GEO-04
        public void WallInFrontOfWall_PlusZ_LinesUpEdges()
        {
            var target = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", target, new Vector3(0.5f, 0.2f, 0.5f), Vector3.forward);

            Assert.AreEqual(new Vector3Int(0, 0, 1), proposal.Min);
            Assert.AreEqual(Alignment.Center, proposal.AlignX);
        }

        [Test] // GEO-10, GEO-11
        public void WallOnWallTop_StacksExactly()
        {
            var target = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", target, new Vector3(0.45f, 2f, 0.3f), Vector3.up);

            Assert.AreEqual(new Vector3Int(0, 4, 0), proposal.Min);
        }

        [TestCase(0.1f, 0, Alignment.Min, TestName = "SideHit_Bottom_AlignsBottoms")]   // GEO-07
        [TestCase(1.0f, 1, Alignment.Center, TestName = "SideHit_Middle_AlignsCentres")] // GEO-08
        [TestCase(1.6f, 2, Alignment.Max, TestName = "SideHit_Top_AlignsTops")]         // GEO-09 (below the top edge band)
        public void SideHit_VerticalIntent(float aimHeight, int expectedMinY, Alignment expected)
        {
            var target = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("block", target, new Vector3(1f, aimHeight, 0.25f), Vector3.right);

            Assert.AreEqual(expectedMinY, proposal.Min.y);
            Assert.AreEqual(expected, proposal.AlignY);
        }

        [Test] // GEO-06
        public void Corner_AimNearFarEnd_PicksEdgeToEdge_NotCentre()
        {
            var longWall = LongWall();

            var proposal = _kit.AimAtPiece("wall", longWall, new Vector3(3.5f, 0.2f, 0.5f), Vector3.forward, GridRotation.R90); // just outside the end band

            Assert.AreEqual(Alignment.Max, proposal.AlignX);
            Assert.AreEqual(longWall.Bounds.Max.x, proposal.Bounds.Max.x, "Corner must be flush with the wall's end.");
            Assert.AreEqual(longWall.Bounds.Max.z, proposal.Bounds.Min.z);
        }

        [Test] // GEO-06
        public void Corner_AimNearNearEnd_PicksEdgeToEdge()
        {
            var longWall = LongWall();

            var proposal = _kit.AimAtPiece("wall", longWall, new Vector3(0.5f, 0.2f, 0.5f), Vector3.forward, GridRotation.R90); // just outside the end band

            Assert.AreEqual(Alignment.Min, proposal.AlignX);
            Assert.AreEqual(longWall.Bounds.Min.x, proposal.Bounds.Min.x);
        }

        [Test] // GEO-19
        public void AimSlightlyOffEdge_WithinMagnet_StillSnapsToEdge()
        {
            var longWall = LongWall();

            var proposal = _kit.AimAtPiece("cube", longWall, new Vector3(3.4f, 0.1f, 0.5f), Vector3.forward);

            Assert.AreEqual(Alignment.Max, proposal.AlignX);
            Assert.AreEqual(longWall.Bounds.Max.x, proposal.Bounds.Max.x);
        }

        [Test] // GEO-20
        public void AimFarFromAnyEdge_SlidesFreelyOnLattice_NoTeleport()
        {
            var longWall = LongWall();

            var proposal = _kit.AimAtPiece("cube", longWall, new Vector3(2.6f, 0.1f, 0.5f), Vector3.forward);

            Assert.AreEqual(Alignment.Free, proposal.AlignX);
            Assert.AreEqual(5, proposal.Min.x, "Cube should sit on the cell under the aim.");
        }

        [Test] // GEO-12
        public void RoofOnWallTop_RestsOnTop()
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("roof", wall, new Vector3(0.5f, 2f, 0.25f), Vector3.up);

            Assert.AreEqual(wall.Bounds.Max.y, proposal.Min.y);
            Assert.AreEqual(0, proposal.Min.x);
        }

        [Test] // GEO-13
        public void RoofBesideRoof_ExtendsWithoutHalfStep()
        {
            _kit.PlaceOrFail("wall", Vector3Int.zero);
            var roof = _kit.PlaceOrFail("roof", new Vector3Int(0, 4, 0));

            var proposal = _kit.AimAtPiece("roof", roof, new Vector3(1f, 2.1f, 0.5f), Vector3.right);

            Assert.AreEqual(new Vector3Int(2, 4, 0), proposal.Min);
        }

        [Test] // GEO-15
        public void UnequalSizes_StayOnTheSameLattice()
        {
            var beam = _kit.PlaceOrFail("beam", Vector3Int.zero); // 3 cells wide: centres can't line up with a 2-cell piece

            var proposal = _kit.AimAtPiece("block", beam, new Vector3(0.75f, 0.25f, 0.5f), Vector3.forward);

            Assert.AreNotEqual(Alignment.Center, proposal.AlignX, "An odd slack cannot centre on the lattice.");
            Assert.IsTrue(proposal.Min.x == 0 || proposal.Min.x == 1);
        }

        [Test] // GEO-18
        public void Rotating_ReSolvesFromTheSameAim()
        {
            var target = _kit.PlaceOrFail("block", Vector3Int.zero);
            var hit = new Vector3(1f, 0.1f, 0.5f);

            var r0 = _kit.AimAtPiece("wall", target, hit, Vector3.right);
            var r90 = _kit.AimAtPiece("wall", target, hit, Vector3.right, GridRotation.R90);

            Assert.AreEqual(2, r0.Min.x);
            Assert.AreEqual(2, r90.Min.x, "Still flush after turning.");
            Assert.AreEqual(new Vector3Int(1, 4, 2), r90.Bounds.Size);
        }

        [Test]
        public void SceneryWall_OffLattice_LeavesGapRatherThanPenetrating()
        {
            var proposal = _kit.Solver.Solve(BuildTestKit.BlockSize, GridRotation.R0,
                BuildAim.AtScene(new Vector3(1.23f, 0.5f, 0f), Vector3.right));

            Assert.AreEqual(Alignment.Contact, proposal.AlignX);
            Assert.GreaterOrEqual(_kit.Grid.CellToWorld(proposal.Min).x, 1.23f);
        }

        [Test]
        public void NothingAimed_HasNoPose()
        {
            Assert.IsFalse(_kit.Solver.Solve(BuildTestKit.BlockSize, GridRotation.R0, BuildAim.Nothing).HasPose);
        }

        private PlacedPiece LongWall() => _kit.PlaceOrFail("long_wall", Vector3Int.zero);
    }
}
