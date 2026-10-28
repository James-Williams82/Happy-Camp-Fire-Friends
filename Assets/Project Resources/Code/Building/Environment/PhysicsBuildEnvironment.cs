using System;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    [Serializable]
    public class PhysicsEnvironmentSettings
    {
        [Tooltip("Colliders that count as ground under a piece.")]
        public LayerMask GroundLayers = ~0;

        [Tooltip("Scenery that blocks building. Placed pieces are ignored here; the build world tracks them exactly.")]
        public LayerMask ObstacleLayers = ~0;

        [Tooltip("Metres. Ground this far above or below a piece's bottom face still supports it, and doesn't block it.")]
        [Min(0.01f)] public float GroundTolerance = 0.3f;

        [Tooltip("Metres shaved off every side of the obstacle test so flush contact with scenery is allowed.")]
        [Min(0f)] public float ContactSkin = 0.02f;
    }

    /// <summary>
    /// <see cref="IBuildEnvironment"/> backed by Unity physics: ground probes under each bottom cell
    /// and an overlap box for scenery. Colliders belonging to placed pieces are skipped so physics
    /// never second-guesses the logical occupancy (doc §12.2).
    /// </summary>
    public sealed class PhysicsBuildEnvironment : IBuildEnvironment
    {
        private readonly BuildGrid _grid;
        private readonly PhysicsEnvironmentSettings _settings;
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Collider[] _overlaps = new Collider[32];

        public PhysicsBuildEnvironment(BuildGrid grid, PhysicsEnvironmentSettings settings)
        {
            _grid = grid;
            _settings = settings ?? new PhysicsEnvironmentSettings();
        }

        public bool HasGroundBelow(Vector3Int cell)
        {
            var bottomCentre = _grid.CellToWorld(cell) + new Vector3(_grid.CellSize * 0.5f, 0f, _grid.CellSize * 0.5f);
            var origin = bottomCentre + Vector3.up * _settings.GroundTolerance;

            int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, _settings.GroundTolerance * 2f,
                _settings.GroundLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!PieceView.IsPartOfPiece(_hits[i].collider)) return true;
            }
            return false;
        }

        public bool IsObstructed(CellBounds bounds)
        {
            var box = _grid.ToWorldBounds(bounds);

            // Raise the bottom so uneven ground within tolerance doesn't count as an obstacle.
            float raise = Mathf.Min(_settings.GroundTolerance, box.size.y * 0.5f);
            var min = box.min + new Vector3(_settings.ContactSkin, raise, _settings.ContactSkin);
            var max = box.max - Vector3.one * _settings.ContactSkin;
            if (max.y <= min.y) max.y = min.y + 0.01f;

            var centre = (min + max) * 0.5f;
            var halfExtents = (max - min) * 0.5f;

            int count = Physics.OverlapBoxNonAlloc(centre, halfExtents, _overlaps, Quaternion.identity,
                _settings.ObstacleLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!PieceView.IsPartOfPiece(_overlaps[i])) return true;
            }
            return false;
        }
    }
}
