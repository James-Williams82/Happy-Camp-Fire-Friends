using CampfireFriends.Building;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>
    /// Edge bands on side faces: aiming just under a piece's top edge stacks on top of it, aiming just
    /// inside a side edge extends the row, and everywhere else keeps face placement.
    /// Bands are 25% of the face, at most 0.3 m: 0.25 m on a 1 m block, 0.3 m under a 2 m wall's top.
    /// </summary>
    public class EdgePlacementTests
    {
        private BuildTestKit _kit;

        [SetUp] public void SetUp() => _kit = new BuildTestKit();
        [TearDown] public void TearDown() => _kit.Dispose();

        [Test]
        public void AimUnderTopEdge_OfBlockSide_StacksOnTop()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("block", block, new Vector3(1f, 0.9f, 0.5f), Vector3.right);

            Assert.AreEqual(new Vector3Int(0, 2, 0), proposal.Min);
            Assert.AreEqual(Vector3Int.up, proposal.Face);
            Assert.AreEqual(Alignment.Contact, proposal.AlignY);
            Assert.AreEqual(PlacementRejection.None, _kit.Check("block", proposal.Min), "A stacked piece must be valid.");
        }

        [Test]
        public void AimUnderTopEdge_OfWallFront_StacksWallOnWall()
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", wall, new Vector3(0.5f, 1.85f, 0.5f), Vector3.forward);

            Assert.AreEqual(new Vector3Int(0, 4, 0), proposal.Min);
            Assert.AreEqual(PlacementRejection.None, _kit.Check("wall", proposal.Min));
        }

        [TestCase(1, TestName = "TopEdge_DeeperPiece_FlushWithFrontFace")]
        [TestCase(-1, TestName = "TopEdge_DeeperPiece_FlushWithBackFace")]
        public void TopEdge_DeeperPiece_StaysFlushWithAimedFace(int side)
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero); // 0.5 m deep; a block is 1 m deep
            float faceZ = side > 0 ? 0.5f : 0f;

            var proposal = _kit.AimAtPiece("block", wall, new Vector3(0.5f, 1.85f, faceZ), new Vector3(0f, 0f, side));

            Assert.AreEqual(wall.Bounds.Max.y, proposal.Min.y);
            if (side > 0) Assert.AreEqual(wall.Bounds.Max.z, proposal.Bounds.Max.z);
            else Assert.AreEqual(wall.Bounds.Min.z, proposal.Bounds.Min.z);
        }

        [Test]
        public void TopEdge_HalfBlock_HasAThinnerBand()
        {
            var cube = _kit.PlaceOrFail("cube", Vector3Int.zero); // 0.5 m: band is 0.125 m

            Assert.AreEqual(new Vector3Int(0, 1, 0), _kit.AimAtPiece("cube", cube, new Vector3(0.5f, 0.45f, 0.25f), Vector3.right).Min);
            Assert.AreEqual(new Vector3Int(1, 0, 0), _kit.AimAtPiece("cube", cube, new Vector3(0.5f, 0.3f, 0.25f), Vector3.right).Min);
        }

        [TestCase(0.95f, 2, TestName = "SideEdge_High_ExtendsRow")]
        [TestCase(0.05f, -2, TestName = "SideEdge_Low_ExtendsRow")]
        public void AimInsideSideEdge_ExtendsRow(float aimX, int expectedMinX)
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", wall, new Vector3(aimX, 1f, 0.5f), Vector3.forward);

            Assert.AreEqual(new Vector3Int(expectedMinX, 0, 0), proposal.Min, "Same base, same front face, right beside it.");
            Assert.AreEqual(Alignment.Contact, proposal.AlignX);
            Assert.AreEqual(PlacementRejection.None, _kit.Check("wall", proposal.Min));
        }

        [Test]
        public void SideEdge_OnRaisedPiece_KeepsItsBase()
        {
            _kit.PlaceOrFail("block", Vector3Int.zero);
            var upper = _kit.PlaceOrFail("block", new Vector3Int(0, 2, 0));

            var proposal = _kit.AimAtPiece("block", upper, new Vector3(0.95f, 1.5f, 1f), Vector3.forward);

            Assert.AreEqual(new Vector3Int(2, 2, 0), proposal.Min);
        }

        [Test]
        public void BottomEdge_KeepsFacePlacement()
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", wall, new Vector3(0.5f, 0.05f, 0.5f), Vector3.forward);

            Assert.AreEqual(new Vector3Int(0, 0, 1), proposal.Min);
            Assert.AreEqual(new Vector3Int(0, 0, 1), proposal.Face);
        }

        [Test]
        public void MiddleOfFace_KeepsFacePlacement()
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", wall, new Vector3(0.5f, 1f, 0.5f), Vector3.forward);

            Assert.AreEqual(1, proposal.Min.z);
            Assert.AreEqual(Alignment.Contact, proposal.AlignZ);
        }

        [TestCase(0.97f, 1.9f, 2, 0, TestName = "Corner_CloserToSide_ExtendsRow")]
        [TestCase(0.8f, 1.97f, 0, 4, TestName = "Corner_CloserToTop_StacksOnTop")]
        public void Corner_NearestEdgeWins(float aimX, float aimY, int expectedMinX, int expectedMinY)
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", wall, new Vector3(aimX, aimY, 0.5f), Vector3.forward);

            Assert.AreEqual(expectedMinX, proposal.Min.x);
            Assert.AreEqual(expectedMinY, proposal.Min.y);
        }

        [Test]
        public void TopFace_IsUnchanged()
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero);

            var proposal = _kit.AimAtPiece("wall", wall, new Vector3(0.95f, 2f, 0.25f), Vector3.up);

            Assert.AreEqual(new Vector3Int(0, 4, 0), proposal.Min, "Near the top face's edge is still a top-face placement.");
        }

        [Test]
        public void EdgeBandsOff_KeepsFacePlacementEverywhere()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);
            var solver = new PlacementSolver(_kit.Grid, _kit.World, new SolverSettings { EdgeBandFraction = 0f });

            var proposal = solver.Solve(BuildTestKit.BlockSize, GridRotation.R0, BuildAim.AtPiece(block.Id, new Vector3(1f, 0.95f, 0.5f), Vector3.right));

            Assert.AreEqual(2, proposal.Min.x);
            Assert.AreEqual(Alignment.Contact, proposal.AlignX);
        }
    }
}
