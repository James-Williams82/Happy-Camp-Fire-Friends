using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using NUnit.Framework;
using UnityEngine;

namespace CampfireFriends.Tests
{
    /// <summary>Using placed pieces with build mode off: a wall light switches on and off for everyone.</summary>
    public class UsePieceTests
    {
        private BuildTestKit _kit;

        [SetUp]
        public void SetUp()
        {
            _kit = new BuildTestKit(boards: 10);
            _kit.Definition("cube").Use = PieceUse.Toggle;
        }

        [TearDown] public void TearDown() => _kit.Dispose();

        [Test]
        public void Use_TogglePiece_SwitchesOnThenOff_AndCostsNothing()
        {
            var light = _kit.PlaceOrFail("cube", Vector3Int.zero);
            int boards = _kit.Stock.Get(ResourceType.Boards);
            Assert.IsFalse(light.On, "placed pieces start off");

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryUse(light.Id));
            Assert.IsTrue(light.On);

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryUse(light.Id));
            Assert.IsFalse(light.On);
            Assert.AreEqual(boards, _kit.Stock.Get(ResourceType.Boards));
        }

        [Test]
        public void Use_RaisesStateChanged_NotRemovedOrAdded()
        {
            var light = _kit.PlaceOrFail("cube", Vector3Int.zero);
            var changed = new List<int>();
            int addsAndRemoves = 0;
            _kit.World.StateChanged += piece => changed.Add(piece.Id);
            _kit.World.Added += _ => addsAndRemoves++;
            _kit.World.Removed += _ => addsAndRemoves++;

            _kit.Service.TryUse(light.Id);

            CollectionAssert.AreEqual(new[] { light.Id }, changed);
            Assert.AreEqual(0, addsAndRemoves);
        }

        [Test]
        public void Use_PieceWithoutAUse_IsNotUsable_AndChangesNothing()
        {
            var block = _kit.PlaceOrFail("block", Vector3Int.zero);

            Assert.AreEqual(PlacementRejection.NotUsable, _kit.Service.TryUse(block.Id));
            Assert.IsFalse(block.On);
        }

        [Test]
        public void Use_UnknownPiece_IsRefused()
        {
            Assert.AreEqual(PlacementRejection.UnknownPlacedPiece, _kit.Service.TryUse(999));
        }

        [Test]
        public void Move_KeepsALightSwitchedOn()
        {
            var light = _kit.PlaceOrFail("cube", Vector3Int.zero);
            _kit.Service.TryUse(light.Id);

            Assert.AreEqual(PlacementRejection.None, _kit.Service.TryMove(light.Id, new Vector3Int(3, 0, 0), GridRotation.R90));

            Assert.IsTrue(_kit.World.TryGet(light.Id, out var moved));
            Assert.IsTrue(moved.On);
        }

        [Test]
        public void SetOn_ToTheSameState_RaisesNothing()
        {
            var light = _kit.PlaceOrFail("cube", Vector3Int.zero);
            int raised = 0;
            _kit.World.StateChanged += _ => raised++;

            Assert.IsTrue(_kit.World.SetOn(light.Id, false));
            Assert.AreEqual(0, raised);
            Assert.IsFalse(_kit.World.SetOn(999, true));
        }
    }
}
