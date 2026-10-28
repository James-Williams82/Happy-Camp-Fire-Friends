using System;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    [Serializable]
    public class TargetingSettings
    {
        [Tooltip("Metres the build pointer reaches (doc Appendix A).")]
        [Min(0.5f)] public float MaxDistance = 8f;

        [Tooltip("What the pointer can hit: ground, scenery and placed pieces.")]
        public LayerMask Layers = ~0;
    }

    /// <summary>What the pointer hit this frame, in both the model's terms (<see cref="Aim"/>) and the scene's.</summary>
    public readonly struct BuildTarget
    {
        public readonly BuildAim Aim;
        public readonly PieceView Piece;
        public readonly Vector3 PointerEnd;

        public BuildTarget(BuildAim aim, PieceView piece, Vector3 pointerEnd)
        {
            Aim = aim;
            Piece = piece;
            PointerEnd = pointerEnd;
        }

        public bool HasHit => Aim.Surface != AimSurface.None;
    }

    /// <summary>Raycasts the pointer into the scene and classifies the hit (doc §8.1). Previews are intangible, so they are never hit.</summary>
    public sealed class BuildTargeter
    {
        private readonly TargetingSettings _settings;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        public BuildTargeter(TargetingSettings settings) => _settings = settings ?? new TargetingSettings();

        /// <param name="ignorePlacedId">A placed piece the pointer passes through: the one being moved.</param>
        public BuildTarget Target(Ray ray, int ignorePlacedId = BuildWorld.NoPiece)
        {
            int count = Physics.RaycastNonAlloc(ray, _hits, _settings.MaxDistance, _settings.Layers, QueryTriggerInteraction.Ignore);

            RaycastHit? nearest = null;
            PieceView nearestPiece = null;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (nearest.HasValue && hit.distance >= nearest.Value.distance) continue;

                var piece = hit.collider.GetComponentInParent<PieceView>();
                if (piece != null && ignorePlacedId != BuildWorld.NoPiece && piece.PlacedId == ignorePlacedId) continue;

                nearest = hit;
                nearestPiece = piece != null && piece.IsPlaced ? piece : null;
            }

            if (!nearest.HasValue) return new BuildTarget(BuildAim.Nothing, null, ray.GetPoint(_settings.MaxDistance));

            var h = nearest.Value;
            return nearestPiece != null
                ? new BuildTarget(BuildAim.AtPiece(nearestPiece.PlacedId, h.point, h.normal), nearestPiece, h.point)
                : new BuildTarget(BuildAim.AtScene(h.point, h.normal), null, h.point);
        }
    }
}
