using System;
using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CampfireFriends.Tests
{
    /// <summary>Fence corners join the fences that meet them end-on: L corners, T junctions and crossings.</summary>
    public class PieceConnectionTests
    {
        private static readonly Vector3Int Fence = new Vector3Int(4, 3, 1);
        private static readonly Vector3Int Corner = new Vector3Int(1, 3, 1);

        private readonly List<PieceDefinition> _definitions = new List<PieceDefinition>();
        private BuildWorld _world;
        private PieceLibrary _pieces;
        private int _nextId;

        [SetUp]
        public void SetUp()
        {
            Define("fence", Fence, "fence");
            Define("corner", Corner, "fence");
            Define("wall", new Vector3Int(2, 4, 1), "");
            _pieces = new PieceLibrary(_definitions);
            _world = new BuildWorld();
            _nextId = 1;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var definition in _definitions) Object.DestroyImmediate(definition);
            _definitions.Clear();
        }

        [Test]
        public void LoneCorner_JoinsNothing()
        {
            var corner = Add("corner", new Vector3Int(4, 0, 0));

            Assert.AreEqual(ConnectedSides.None, PieceConnections.Of(_world, _pieces, corner));
        }

        [Test]
        public void LCorner_JoinsBothFences()
        {
            Add("fence", new Vector3Int(0, 0, 0));                              // along x, ends at x = 4
            var corner = Add("corner", new Vector3Int(4, 0, 0));
            Add("fence", new Vector3Int(4, 0, 1), GridRotation.R90);            // along z, starts at z = 1

            Assert.AreEqual(ConnectedSides.NegX | ConnectedSides.PosZ, PieceConnections.Of(_world, _pieces, corner));
        }

        [Test]
        public void Crossing_JoinsAllFourSides()
        {
            var corner = Add("corner", new Vector3Int(4, 0, 4));
            Add("fence", new Vector3Int(0, 0, 4));
            Add("fence", new Vector3Int(5, 0, 4));
            Add("fence", new Vector3Int(4, 0, 0), GridRotation.R90);
            Add("fence", new Vector3Int(4, 0, 5), GridRotation.R90);

            Assert.AreEqual(ConnectedSides.PosX | ConnectedSides.NegX | ConnectedSides.PosZ | ConnectedSides.NegZ,
                PieceConnections.Of(_world, _pieces, corner));
        }

        [Test]
        public void FenceRunningPastTheSide_DoesNotJoin()
        {
            var corner = Add("corner", new Vector3Int(4, 0, 0));
            Add("fence", new Vector3Int(2, 0, 1));                              // broadside along the corner's +z face

            Assert.AreEqual(ConnectedSides.None, PieceConnections.Of(_world, _pieces, corner));
        }

        [Test]
        public void TwoCorners_JoinEachOther()
        {
            var a = Add("corner", new Vector3Int(0, 0, 0));
            var b = Add("corner", new Vector3Int(1, 0, 0));

            Assert.AreEqual(ConnectedSides.PosX, PieceConnections.Of(_world, _pieces, a));
            Assert.AreEqual(ConnectedSides.NegX, PieceConnections.Of(_world, _pieces, b));
        }

        [Test]
        public void OtherGroups_DoNotJoin_AndRemovalUnjoins()
        {
            var corner = Add("corner", new Vector3Int(4, 0, 0));
            Add("wall", new Vector3Int(4, 0, 1), GridRotation.R90);
            var fence = Add("fence", new Vector3Int(0, 0, 0));
            Assert.AreEqual(ConnectedSides.NegX, PieceConnections.Of(_world, _pieces, corner));

            _world.Remove(fence.Id);
            Assert.AreEqual(ConnectedSides.None, PieceConnections.Of(_world, _pieces, corner));
        }

        private PlacedPiece Add(string id, Vector3Int min, GridRotation rotation = GridRotation.R0)
        {
            _pieces.TryGet(id, out var definition);
            var piece = new PlacedPiece(_nextId++, id, definition.BoundsAt(min, rotation), rotation, 1, 100);
            _world.Add(piece);
            return piece;
        }

        private void Define(string id, Vector3Int footprint, string group)
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>();
            definition.Id = id;
            definition.Footprint = footprint;
            definition.ConnectGroup = group;
            definition.Cost = new List<ResourceAmount>();
            _definitions.Add(definition);
        }
    }
}
