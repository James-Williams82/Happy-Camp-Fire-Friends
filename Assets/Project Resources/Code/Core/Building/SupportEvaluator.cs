using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// Decides whether a box would be held up, using only logical cells and the environment's ground
    /// probe. Support is a separate question from overlap (doc §12.3): a flush but floating piece is
    /// correctly positioned and unsupported, not moved somewhere else.
    /// </summary>
    public sealed class SupportEvaluator
    {
        private readonly BuildWorld _world;
        private readonly IPieceLookup _pieces;
        private readonly IBuildEnvironment _environment;

        public SupportEvaluator(BuildWorld world, IPieceLookup pieces, IBuildEnvironment environment)
        {
            _world = world;
            _pieces = pieces;
            _environment = environment;
        }

        /// <param name="self">The piece being checked, if it is already in the world.</param>
        /// <param name="removed">A piece to treat as gone, when asking "what if this were removed?".</param>
        public bool IsSupported(SupportRule rule, CellBounds bounds, int self = BuildWorld.NoPiece, int removed = BuildWorld.NoPiece)
        {
            switch (rule)
            {
                case SupportRule.None:
                    return true;

                case SupportRule.Ground:
                    foreach (var cell in bounds.BottomCells())
                    {
                        if (!_environment.HasGroundBelow(cell)) return false;
                    }
                    return true;

                case SupportRule.Stack:
                    foreach (var cell in bounds.BottomCells())
                    {
                        if (!HasPieceBelow(cell, removed) && !_environment.HasGroundBelow(cell)) return false;
                    }
                    return true;

                case SupportRule.SideAttach:
                    foreach (var cell in bounds.BottomCells())
                    {
                        if (HasPieceBelow(cell, removed) || _environment.HasGroundBelow(cell)) return true;
                    }
                    foreach (var cell in bounds.SideNeighbours(bounds.Min.y, bounds.Max.y))
                    {
                        if (_world.PieceAt(cell, removed) != BuildWorld.NoPiece) return true;
                    }
                    return false;

                case SupportRule.Roof:
                    return IsRoofSupported(bounds, self, removed);

                default:
                    return false;
            }
        }

        private bool HasPieceBelow(Vector3Int cell, int removed) =>
            _world.PieceAt(cell + Vector3Int.down, removed) != BuildWorld.NoPiece;

        private bool RestsOnAnyPiece(CellBounds bounds, int removed)
        {
            foreach (var cell in bounds.BottomCells())
            {
                if (HasPieceBelow(cell, removed)) return true;
            }
            return false;
        }

        /// <summary>
        /// A roof is held up if it rests on a piece, or if it is part of a run of edge-sharing roof
        /// panels at the same height in which at least one panel rests on a piece. Side neighbours
        /// share a whole cell face, so corner-only contact can never join a run.
        /// </summary>
        private bool IsRoofSupported(CellBounds bounds, int self, int removed)
        {
            if (RestsOnAnyPiece(bounds, removed)) return true;

            var visited = new HashSet<int> { self, removed };
            var frontier = new Queue<CellBounds>();
            frontier.Enqueue(bounds);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var cell in current.SideNeighbours(current.Min.y, current.Min.y + 1))
                {
                    int id = _world.PieceAt(cell, removed);
                    if (id == BuildWorld.NoPiece || !visited.Add(id)) continue;
                    if (!_world.TryGet(id, out var neighbour) || !IsRoof(neighbour) || neighbour.Bounds.Min.y != bounds.Min.y) continue;

                    if (RestsOnAnyPiece(neighbour.Bounds, removed)) return true;
                    frontier.Enqueue(neighbour.Bounds);
                }
            }
            return false;
        }

        private bool IsRoof(PlacedPiece piece) =>
            _pieces.TryGet(piece.PieceId, out var definition) && definition.Support == SupportRule.Roof;
    }
}
