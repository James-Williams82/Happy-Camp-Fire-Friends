using CampfireFriends.Building;
using CampfireFriends.Game;
using CampfireFriends.Supplies;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Creative mode: build freely, with every placement rule but the cost still in force.</summary>
    public class GameModeTests
    {
        private BuildTestKit _kit;

        [SetUp] public void SetUp() => _kit = new BuildTestKit(boards: 0);
        [TearDown] public void TearDown() => _kit.Dispose();

        [Test]
        public void Creative_BuildsFree_AndSurvival_IsNotAvailableYet()
        {
            Assert.IsTrue(GameRules.For(GameMode.Creative).Available);
            Assert.IsTrue(GameRules.For(GameMode.Creative).FreeBuilding);
            Assert.IsFalse(GameRules.For(GameMode.Survival).Available);
            Assert.IsFalse(GameRules.For(GameMode.Survival).FreeBuilding);
        }

        [Test]
        public void FreeBuilding_PlacesWithNoStock_AndSpendsNothing()
        {
            _kit.Service.FreeBuilding = true;

            var result = _kit.Place("block", Vector3Int.zero);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, _kit.Stock.Get(ResourceType.Boards));
        }

        [Test]
        public void WithoutFreeBuilding_AnEmptyStockRefuses()
        {
            var result = _kit.Place("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.InsufficientMaterials, result.Rejection);
        }

        [Test]
        public void FreeBuilding_RemovingRefundsNothing()
        {
            _kit.Service.FreeBuilding = true;
            var piece = _kit.Place("block", Vector3Int.zero).Piece;

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryRemove(piece.Id));
            Assert.AreEqual(0, _kit.Stock.Get(ResourceType.Boards), "Free pieces must not mint materials when taken down.");
        }

        [Test]
        public void FreeBuilding_StillEnforcesPlacementRules()
        {
            _kit.Service.FreeBuilding = true;

            Assert.IsTrue(_kit.Place("block", Vector3Int.zero).Success);
            Assert.AreEqual(PlacementRejection.Occupied, _kit.Place("block", Vector3Int.zero).Rejection);
            Assert.AreEqual(PlacementRejection.NoSupport, _kit.Place("block", new Vector3Int(6, 5, 0)).Rejection);
        }
    }
}
