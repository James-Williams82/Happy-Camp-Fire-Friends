using System;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>The four horizontal sides of a piece, in world (grid) directions.</summary>
    [Flags]
    public enum ConnectedSides
    {
        None = 0,
        PosX = 1,
        NegX = 2,
        PosZ = 4,
        NegZ = 8,
    }

    /// <summary>
    /// Which sides of a placed piece join up with neighbours of the same <see cref="PieceDefinition.ConnectGroup"/>,
    /// so a fence corner can grow boards toward the fences meeting it: an L, a T or a crossing.
    /// A neighbour joins only end-on (no wider across the shared face than this piece), so a fence
    /// running past a post's side doesn't count.
    /// </summary>
    public static class PieceConnections
    {
        public static ConnectedSides Of(BuildWorld world, IPieceLookup pieces, PlacedPiece piece)
        {
            if (!pieces.TryGet(piece.PieceId, out var definition) || string.IsNullOrEmpty(definition.ConnectGroup))
                return ConnectedSides.None;

            var b = piece.Bounds;
            var sides = ConnectedSides.None;
            if (Joins(world, pieces, definition.ConnectGroup, piece, new Vector3Int(b.Max.x, 0, 0), true)) sides |= ConnectedSides.PosX;
            if (Joins(world, pieces, definition.ConnectGroup, piece, new Vector3Int(b.Min.x - 1, 0, 0), true)) sides |= ConnectedSides.NegX;
            if (Joins(world, pieces, definition.ConnectGroup, piece, new Vector3Int(0, 0, b.Max.z), false)) sides |= ConnectedSides.PosZ;
            if (Joins(world, pieces, definition.ConnectGroup, piece, new Vector3Int(0, 0, b.Min.z - 1), false)) sides |= ConnectedSides.NegZ;
            return sides;
        }

        /// <summary>The unit grid direction of one side.</summary>
        public static Vector3Int Direction(ConnectedSides side)
        {
            switch (side)
            {
                case ConnectedSides.PosX: return Vector3Int.right;
                case ConnectedSides.NegX: return Vector3Int.left;
                case ConnectedSides.PosZ: return new Vector3Int(0, 0, 1);
                case ConnectedSides.NegZ: return new Vector3Int(0, 0, -1);
                default: return Vector3Int.zero;
            }
        }

        /// <param name="plane">The layer of cells beside the face: its x (when <paramref name="xFace"/>) or z coordinate.</param>
        private static bool Joins(BuildWorld world, IPieceLookup pieces, string group, PlacedPiece piece, Vector3Int plane, bool xFace)
        {
            var b = piece.Bounds;
            int from = xFace ? b.Min.z : b.Min.x, to = xFace ? b.Max.z : b.Max.x;

            for (int y = b.Min.y; y < b.Max.y; y++)
            for (int t = from; t < to; t++)
            {
                var cell = xFace ? new Vector3Int(plane.x, y, t) : new Vector3Int(t, y, plane.z);
                int id = world.PieceAt(cell);
                if (id == BuildWorld.NoPiece || id == piece.Id || !world.TryGet(id, out var neighbour)) continue;
                if (!pieces.TryGet(neighbour.PieceId, out var other) || other.ConnectGroup != group) continue;

                // End-on only: across the face, the neighbour is no wider than this piece.
                int across = xFace ? neighbour.Bounds.Size.z : neighbour.Bounds.Size.x;
                if (across <= to - from) return true;
            }
            return false;
        }
    }
}
