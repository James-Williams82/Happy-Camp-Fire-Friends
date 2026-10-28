using CampfireFriends.Building;
using CampfireFriends.Supplies;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Validation and support tests (doc §12, VAL-01…VAL-05, GEO-12…GEO-14).</summary>
    public class PlacementValidationTests
    {
        private BuildTestKit _kit;

        [SetUp] public void SetUp() => _kit = new BuildTestKit();
        [TearDown] public void TearDown() => _kit.Dispose();

        [Test] // GEO-01
        public void FirstPieceOnGround_IsValid()
        {
            Assert.AreEqual(PlacementRejection.None, _kit.Check("block", Vector3Int.zero));
        }

        [Test] // VAL-01
        public void IntersectingOccupiedCells_IsOccupied()
        {
            _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.Occupied, _kit.Check("block", new Vector3Int(1, 0, 1)));
        }

        [Test] // VAL-02
        public void SharingOnlyAFace_IsNotOverlap()
        {
            _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.None, _kit.Check("block", new Vector3Int(2, 0, 0)));
            Assert.AreEqual(PlacementRejection.None, _kit.Check("block", new Vector3Int(0, 2, 0)));
        }

        [Test] // VAL-03
        public void FloatingInEmptyAir_IsUnsupported()
        {
            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("block", new Vector3Int(0, 3, 0)));
        }

        [Test]
        public void Stack_NeedsSomethingUnderEveryBottomCell()
        {
            _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.None, _kit.Check("block", new Vector3Int(0, 2, 0)));
            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("block", new Vector3Int(1, 2, 0)), "Half overhanging the block below.");
        }

        [Test]
        public void GroundRule_RejectsRestingOnAPiece()
        {
            _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("foundation", new Vector3Int(0, 2, 0)));
            Assert.AreEqual(PlacementRejection.None, _kit.Check("foundation", new Vector3Int(4, 0, 0)));
        }

        [Test] // VAL-04
        public void RoofOnGround_IsUnsupported()
        {
            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("roof", Vector3Int.zero));
        }

        [Test] // GEO-12
        public void RoofOnWall_IsSupported()
        {
            _kit.PlaceOrFail("wall", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.None, _kit.Check("roof", new Vector3Int(0, 4, 0)));
        }

        [Test] // GEO-13
        public void RoofRun_IsHeldUpByOneSupportedPanel()
        {
            _kit.PlaceOrFail("wall", Vector3Int.zero);
            _kit.PlaceOrFail("roof", new Vector3Int(0, 4, 0));
            _kit.PlaceOrFail("roof", new Vector3Int(2, 4, 0));

            Assert.AreEqual(PlacementRejection.None, _kit.Check("roof", new Vector3Int(4, 4, 0)), "Third panel in the run.");
            Assert.AreEqual(PlacementRejection.None, _kit.Check("roof", new Vector3Int(2, 4, 2)), "Panel beside the second.");
        }

        [Test] // GEO-14
        public void RoofTouchingOnlyAtACorner_IsUnsupported()
        {
            _kit.PlaceOrFail("wall", Vector3Int.zero);
            _kit.PlaceOrFail("roof", new Vector3Int(0, 4, 0));

            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("roof", new Vector3Int(2, 4, 2)));
        }

        [Test]
        public void RoofAtDifferentHeight_DoesNotJoinTheRun()
        {
            _kit.PlaceOrFail("wall", Vector3Int.zero);
            _kit.PlaceOrFail("roof", new Vector3Int(0, 4, 0));

            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("roof", new Vector3Int(2, 3, 0)));
        }

        [Test]
        public void SideAttach_HangsOffAPieceButNotOffNothing()
        {
            _kit.PlaceOrFail("wall", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.None, _kit.Check("beam", new Vector3Int(2, 3, 0)));
            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Check("beam", new Vector3Int(6, 3, 0)));
        }

        [Test] // VAL-05
        public void NotEnoughBoards_IsReportedBeforeCommit()
        {
            using (var poor = new BuildTestKit(boards: 1))
            {
                Assert.AreEqual(PlacementRejection.InsufficientMaterials, poor.Check("block", Vector3Int.zero));
            }
        }

        [Test]
        public void Scenery_InTheWay_IsObstructed()
        {
            var environment = new ScriptedEnvironment();
            environment.Obstacles.Add(new CellBounds(new Vector3Int(1, 0, 1), Vector3Int.one));

            using (var kit = new BuildTestKit(environment: environment))
            {
                Assert.AreEqual(PlacementRejection.Obstructed, kit.Check("block", Vector3Int.zero));
                Assert.AreEqual(PlacementRejection.None, kit.Check("block", new Vector3Int(2, 0, 0)));
            }
        }

        [Test]
        public void UnknownPiece_IsRejected()
        {
            Assert.AreEqual(PlacementRejection.UnknownPiece, _kit.Check("nope", Vector3Int.zero));
        }

        [Test]
        public void PreviewWithoutStock_SkipsCostCheck()
        {
            using (var poor = new BuildTestKit(boards: 0))
            {
                Assert.AreEqual(PlacementRejection.None, poor.Validator.ValidatePlacement("block", Vector3Int.zero, GridRotation.R0, null));
            }
        }
    }
}
