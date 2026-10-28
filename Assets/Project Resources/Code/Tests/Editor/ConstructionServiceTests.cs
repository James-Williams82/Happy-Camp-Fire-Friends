using System;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Lifecycle and transaction tests (doc §14.4, §14.7, VAL-06…VAL-12).</summary>
    public class ConstructionServiceTests
    {
        private BuildTestKit _kit;

        [SetUp] public void SetUp() => _kit = new BuildTestKit(boards: 10);
        [TearDown] public void TearDown() => _kit.Dispose();

        private int Boards => _kit.Stock.Get(ResourceType.Boards);

        [Test]
        public void Place_SpendsOnce_AndClaimsFootprint()
        {
            var result = _kit.Place("wall", Vector3Int.zero);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(7, Boards);
            Assert.AreEqual(result.Piece.Id, _kit.World.PieceAt(new Vector3Int(1, 3, 0)));
            Assert.AreEqual(BuildWorld.NoPiece, _kit.World.PieceAt(new Vector3Int(2, 0, 0)));
        }

        [Test] // VAL-12
        public void ContestedSpot_IsOnlyBuiltOnce_AndLoserPaysNothing()
        {
            var first = _kit.Place("block", Vector3Int.zero, player: 1);
            var second = _kit.Place("block", Vector3Int.zero, player: 2);

            Assert.IsTrue(first.Success);
            Assert.AreEqual(PlacementRejection.Occupied, second.Rejection);
            Assert.AreEqual(1, _kit.World.Count);
            Assert.AreEqual(8, Boards);
        }

        [Test]
        public void FailedPlacement_ChangesNothing()
        {
            var rejected = PlacementRejection.None;
            _kit.Service.PlaceRejected += (_, reason) => rejected = reason;

            var result = _kit.Place("block", new Vector3Int(0, 5, 0));

            Assert.AreEqual(PlacementRejection.NoSupport, result.Rejection);
            Assert.AreEqual(PlacementRejection.NoSupport, rejected);
            Assert.AreEqual(10, Boards);
            Assert.AreEqual(0, _kit.World.Count);
        }

        [Test] // VAL-07
        public void Remove_ReleasesCells_AndRefundsOnce()
        {
            var piece = _kit.Place("block", Vector3Int.zero).Piece;

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryRemove(piece.Id));
            Assert.AreEqual(PlacementRejection.UnknownPlacedPiece, _kit.Service.TryRemove(piece.Id));

            Assert.AreEqual(10, Boards);
            Assert.IsTrue(_kit.World.IsFree(piece.Bounds));
            Assert.AreEqual(PlacementRejection.None, _kit.Check("block", Vector3Int.zero), "A cleared spot is buildable again.");
        }

        [Test] // VAL-08
        public void RemovingASupport_IsBlockedWhileSomethingRestsOnIt()
        {
            var bottom = _kit.PlaceOrFail("block", Vector3Int.zero);
            var top = _kit.PlaceOrFail("block", new Vector3Int(0, 2, 0));

            Assert.AreEqual(PlacementRejection.HasDependents, _kit.Service.TryRemove(bottom.Id));
            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryRemove(top.Id));
            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryRemove(bottom.Id));
        }

        [Test] // VAL-08
        public void RemovingTheWallUnderARoofRun_IsBlocked()
        {
            using (var kit = new BuildTestKit())
            {
                var wall = kit.PlaceOrFail("wall", Vector3Int.zero);
                kit.PlaceOrFail("roof", new Vector3Int(0, 4, 0));
                kit.PlaceOrFail("roof", new Vector3Int(2, 4, 0));

                Assert.AreEqual(PlacementRejection.HasDependents, kit.Service.TryRemove(wall.Id));
            }
        }

        [Test]
        public void RemovingTheMiddleOfARoofRun_IsBlocked_RemovingTheEndIsNot()
        {
            using (var kit = new BuildTestKit())
            {
                kit.PlaceOrFail("wall", Vector3Int.zero);
                kit.PlaceOrFail("roof", new Vector3Int(0, 4, 0));
                var middle = kit.PlaceOrFail("roof", new Vector3Int(2, 4, 0));
                var end = kit.PlaceOrFail("roof", new Vector3Int(4, 4, 0));

                Assert.AreEqual(PlacementRejection.HasDependents, kit.Service.TryRemove(middle.Id));
                Assert.AreEqual(PlacementRejection.None, kit.Service.TryRemove(end.Id));
                Assert.AreEqual(PlacementRejection.None, kit.Service.TryRemove(middle.Id));
            }
        }

        [Test]
        public void NonRemovablePiece_StaysPut()
        {
            var piece = _kit.PlaceOrFail("fixed", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.NotRemovable, _kit.Service.TryRemove(piece.Id));
        }

        [Test] // VAL-09
        public void ReplacingRotated_LeaksNoClaims()
        {
            var piece = _kit.PlaceOrFail("wall", Vector3Int.zero);
            _kit.Service.TryRemove(piece.Id);

            var turned = _kit.Place("wall", Vector3Int.zero, GridRotation.R90);

            Assert.IsTrue(turned.Success);
            Assert.AreEqual(BuildWorld.NoPiece, _kit.World.PieceAt(new Vector3Int(1, 0, 0)), "The old 0° footprint must be released.");
            Assert.AreEqual(turned.Piece.Id, _kit.World.PieceAt(new Vector3Int(0, 0, 1)));
        }

        [Test] // VAL-10
        public void Restore_KeepsIdsAndPose_AndChargesNothing()
        {
            var saved = new PlacedPiece(7, "wall", CellBounds.ForPiece(new Vector3Int(4, 0, 4), BuildTestKit.WallSize, GridRotation.R90), GridRotation.R90, 2, 50);

            _kit.Service.Restore(saved);
            var next = _kit.Place("block", Vector3Int.zero);

            Assert.AreEqual(10 - 2, Boards);
            Assert.AreEqual(7, _kit.World.PieceAt(new Vector3Int(4, 3, 5)));
            Assert.Greater(next.Piece.Id, 7, "New ids continue after restored ones.");
        }

        [Test] // VAL-11
        public void Restore_UnknownPieceOrWrongFootprint_FailsLoudly()
        {
            Assert.Throws<ArgumentException>(() => _kit.Service.Restore(
                new PlacedPiece(1, "gone", new CellBounds(Vector3Int.zero, Vector3Int.one), GridRotation.R0, 0, 1)));
            Assert.Throws<ArgumentException>(() => _kit.Service.Restore(
                new PlacedPiece(2, "wall", new CellBounds(Vector3Int.zero, Vector3Int.one), GridRotation.R0, 0, 1)));
            Assert.AreEqual(0, _kit.World.Count);
        }

        [Test]
        public void World_NeverAllowsTwoClaimsOnOneCell()
        {
            _kit.World.Add(new PlacedPiece(1, "cube", new CellBounds(Vector3Int.zero, Vector3Int.one), GridRotation.R0, 0, 1));

            Assert.Throws<InvalidOperationException>(() =>
                _kit.World.Add(new PlacedPiece(2, "cube", new CellBounds(Vector3Int.zero, Vector3Int.one), GridRotation.R0, 0, 1)));
        }
    }
}
