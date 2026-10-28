using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Repositioning and turning placed pieces from the modify menu.</summary>
    public class MovePieceTests
    {
        private BuildTestKit _kit;

        [SetUp] public void SetUp() => _kit = new BuildTestKit(boards: 10);
        [TearDown] public void TearDown() => _kit.Dispose();

        [Test]
        public void Move_KeepsIdOwnerAndHealth_FreesOldCells_CostsNothing()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);
            block.Health = 37;
            int boards = _kit.Stock.Get(ResourceType.Boards);

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryMove(block.Id, new Vector3Int(4, 0, 0), GridRotation.R0));

            Assert.IsTrue(_kit.World.TryGet(block.Id, out var moved));
            Assert.AreEqual(new Vector3Int(4, 0, 0), moved.Bounds.Min);
            Assert.AreEqual(block.PlacedBy, moved.PlacedBy);
            Assert.AreEqual(37, moved.Health);
            Assert.AreEqual(BuildWorld.NoPiece, _kit.World.PieceAt(Vector3Int.zero));
            Assert.AreEqual(boards, _kit.Stock.Get(ResourceType.Boards));
        }

        [Test]
        public void Move_ByOneCell_MayOverlapItsOwnOldCells()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryMove(block.Id, new Vector3Int(1, 0, 0), GridRotation.R0));
        }

        [Test]
        public void Move_OntoItself_HasNoSupport()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Service.TryMove(block.Id, new Vector3Int(0, 2, 0), GridRotation.R0));
        }

        [Test]
        public void Move_WhileSomethingRestsOnIt_IsRefused_AndChangesNothing()
        {
            var bottom = _kit.PlaceOrFail("block", Vector3Int.zero);
            _kit.PlaceOrFail("block", new Vector3Int(0, 2, 0));

            Assert.AreEqual(PlacementRejection.HasDependents, _kit.Service.TryMove(bottom.Id, new Vector3Int(4, 0, 0), GridRotation.R0));
            Assert.AreEqual(bottom.Id, _kit.World.PieceAt(Vector3Int.zero));
        }

        [Test]
        public void Move_IntoAnotherPiece_IsOccupied_AndChangesNothing()
        {
            var a = _kit.PlaceOrFail("block", Vector3Int.zero);
            _kit.PlaceOrFail("block", new Vector3Int(4, 0, 0));

            Assert.AreEqual(PlacementRejection.Occupied, _kit.Service.TryMove(a.Id, new Vector3Int(3, 0, 0), GridRotation.R0));
            Assert.AreEqual(a.Id, _kit.World.PieceAt(Vector3Int.zero));
        }

        [Test]
        public void Move_TopOfAStack_ToTheGround_IsAllowed()
        {
            _kit.PlaceOrFail("block", Vector3Int.zero);
            var top = _kit.PlaceOrFail("block", new Vector3Int(0, 2, 0));

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryMove(top.Id, new Vector3Int(2, 0, 0), GridRotation.R0));
        }

        [Test]
        public void Move_NonRemovablePiece_IsRefused()
        {
            var fixedPiece = _kit.PlaceOrFail("fixed", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.NotRemovable, _kit.Service.TryMove(fixedPiece.Id, new Vector3Int(2, 0, 0), GridRotation.R0));
        }

        [Test]
        public void Move_AnnouncesRemoveThenAdd_WithTheSameId()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);
            var events = new List<string>();
            _kit.World.Removed += p => events.Add($"-{p.Id}");
            _kit.World.Added += p => events.Add($"+{p.Id}");

            _kit.Service.TryMove(block.Id, new Vector3Int(4, 0, 0), GridRotation.R0);

            CollectionAssert.AreEqual(new[] { $"-{block.Id}", $"+{block.Id}" }, events);
        }

        [Test]
        public void TurnInPlace_KeepsBaseAndCentre_AsCloseAsTheLatticeAllows()
        {
            var wall = _kit.PlaceOrFail("wall", Vector3Int.zero); // 2 wide × 1 deep

            var min = wall.Bounds.MinForSizeAboutCentre(GridRotation.R90.RotateSize(BuildTestKit.WallSize));
            Assert.AreEqual(new Vector3Int(0, 0, -1), min);

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryMove(wall.Id, min, GridRotation.R90));
            _kit.World.TryGet(wall.Id, out var turned);
            Assert.AreEqual(GridRotation.R90, turned.Rotation);
            Assert.AreEqual(new Vector3Int(1, 4, 2), turned.Bounds.Size);
        }

        [Test]
        public void TurnInPlace_SquarePiece_StaysPut()
        {
            var block = _kit.PlaceOrFail("block", new Vector3Int(3, 0, 5));

            Assert.AreEqual(block.Bounds.Min, block.Bounds.MinForSizeAboutCentre(GridRotation.R90.RotateSize(BuildTestKit.BlockSize)));
        }
    }
}
