using System.Collections.Generic;
using CampfireFriends.Supplies;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// Answers "is this exact placement or removal allowed?" without changing anything. The local
    /// preview and the host's commit both call this, so a green ghost and the real piece always agree
    /// (doc §17.4). Checks run in a fixed order and the first failure is reported.
    /// </summary>
    public sealed class PlacementValidator
    {
        private readonly BuildWorld _world;
        private readonly IPieceLookup _pieces;
        private readonly IBuildEnvironment _environment;
        private readonly SupportEvaluator _support;

        public PlacementValidator(BuildWorld world, IPieceLookup pieces, IBuildEnvironment environment)
        {
            _world = world;
            _pieces = pieces;
            _environment = environment;
            _support = new SupportEvaluator(world, pieces, environment);
        }

        public bool TryGetDefinition(string pieceId, out PieceDefinition definition) => _pieces.TryGet(pieceId, out definition);

        /// <param name="stock">The stock to check the cost against, or null to skip the cost check.</param>
        public PlacementRejection ValidatePlacement(string pieceId, Vector3Int min, GridRotation rotation, IStockView stock)
        {
            if (!_pieces.TryGet(pieceId, out var definition)) return PlacementRejection.UnknownPiece;

            var bounds = definition.BoundsAt(min, rotation);
            if (!_world.IsFree(bounds)) return PlacementRejection.Occupied;
            if (_environment.IsObstructed(bounds)) return PlacementRejection.Obstructed;
            if (!_support.IsSupported(definition.Support, bounds)) return PlacementRejection.NoSupport;
            if (stock != null && !stock.CanAfford(definition.Cost)) return PlacementRejection.InsufficientMaterials;

            return PlacementRejection.None;
        }

        /// <summary>
        /// Removal is refused while anything depends on the piece (doc §12.6, v0.2/v0.3 conservative
        /// policy): the world never ends up with a floating piece or a stale claim.
        /// </summary>
        public PlacementRejection ValidateRemoval(int placedId)
        {
            if (!_world.TryGet(placedId, out var piece)) return PlacementRejection.UnknownPlacedPiece;
            if (!_pieces.TryGet(piece.PieceId, out var definition)) return PlacementRejection.UnknownPiece;
            if (!definition.Removable) return PlacementRejection.NotRemovable;
            if (FindDependent(piece) != BuildWorld.NoPiece) return PlacementRejection.HasDependents;

            return PlacementRejection.None;
        }

        /// <summary>The first touching piece that would lose its support if <paramref name="piece"/> were removed.</summary>
        private int FindDependent(PlacedPiece piece)
        {
            var checkedIds = new HashSet<int> { piece.Id };
            foreach (var cell in piece.Bounds.FaceNeighbours())
            {
                int id = _world.PieceAt(cell);
                if (id == BuildWorld.NoPiece || !checkedIds.Add(id)) continue;
                if (!_world.TryGet(id, out var neighbour) || !_pieces.TryGet(neighbour.PieceId, out var definition)) continue;

                if (!_support.IsSupported(definition.Support, neighbour.Bounds, self: neighbour.Id, removed: piece.Id))
                    return neighbour.Id;
            }
            return BuildWorld.NoPiece;
        }
    }
}
