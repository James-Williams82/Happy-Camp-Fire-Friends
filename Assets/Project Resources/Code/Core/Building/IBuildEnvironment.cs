using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// What the static scene contributes to a placement: ground to stand on, and scenery in the way.
    /// Placed pieces are not part of the environment; the <see cref="BuildWorld"/> tracks those exactly.
    /// </summary>
    public interface IBuildEnvironment
    {
        /// <summary>Whether there is ground directly under the bottom face of <paramref name="cell"/>.</summary>
        bool HasGroundBelow(Vector3Int cell);

        /// <summary>Whether scenery (not placed pieces) intersects the box.</summary>
        bool IsObstructed(CellBounds bounds);
    }

    /// <summary>Flat ground at a fixed cell height and nothing in the way. For tests and headless use.</summary>
    public sealed class FlatGroundEnvironment : IBuildEnvironment
    {
        private readonly int _groundLevel;

        /// <param name="groundLevel">The cell layer whose bottom face sits on the ground.</param>
        public FlatGroundEnvironment(int groundLevel = 0) => _groundLevel = groundLevel;

        public bool HasGroundBelow(Vector3Int cell) => cell.y == _groundLevel;

        public bool IsObstructed(CellBounds bounds) => bounds.Min.y < _groundLevel;
    }
}
