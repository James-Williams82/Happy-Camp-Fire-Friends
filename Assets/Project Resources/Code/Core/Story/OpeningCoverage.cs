using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// How well a doorway is boarded up (doc: "coverage + support, no exact-piece dependency").
    /// The opening is a face of lattice cells; a cell counts as covered when any placed piece fills
    /// it or the cells just in front of or behind it. Placement rules already guarantee support.
    /// </summary>
    public sealed class OpeningCoverage
    {
        private readonly Vector3Int _min;
        private readonly Vector3Int _max;      // exclusive
        private readonly Vector3Int _depth;    // the axis through the opening, as a unit step

        /// <param name="min">The opening's lowest cell (in the doorway's plane).</param>
        /// <param name="max">Exclusive upper corner of the opening's cells.</param>
        /// <param name="through">A unit step through the opening (e.g. +X for a door in an east wall).</param>
        public OpeningCoverage(Vector3Int min, Vector3Int max, Vector3Int through)
        {
            _min = min;
            _max = max;
            _depth = through;
        }

        public int CellCount => Mathf.Max(0, (_max.x - _min.x) * (_max.y - _min.y) * (_max.z - _min.z));

        /// <summary>Fraction 0–1 of the opening's cells covered, and how many pieces are involved.</summary>
        public float Covered(BuildWorld world, out int pieces)
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            int covered = 0;
            for (int x = _min.x; x < _max.x; x++)
            for (int y = _min.y; y < _max.y; y++)
            for (int z = _min.z; z < _max.z; z++)
            {
                var cell = new Vector3Int(x, y, z);
                bool hit = false;
                foreach (var step in new[] { Vector3Int.zero, _depth, -_depth })
                {
                    int id = world.PieceAt(cell + step);
                    if (id == BuildWorld.NoPiece) continue;
                    seen.Add(id);
                    hit = true;
                }
                if (hit) covered++;
            }

            pieces = seen.Count;
            return CellCount > 0 ? covered / (float)CellCount : 0f;
        }
    }
}
