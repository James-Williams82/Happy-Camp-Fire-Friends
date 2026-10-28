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

        public BuildTargeter(TargetingSettings settings) => _settings = settings ?? new TargetingSettings();

        public BuildTarget Target(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hit, _settings.MaxDistance, _settings.Layers, QueryTriggerInteraction.Ignore))
                return new BuildTarget(BuildAim.Nothing, null, ray.GetPoint(_settings.MaxDistance));

            var piece = hit.collider.GetComponentInParent<PieceView>();
            if (piece != null && piece.IsPlaced)
                return new BuildTarget(BuildAim.AtPiece(piece.PlacedId, hit.point, hit.normal), piece, hit.point);

            return new BuildTarget(BuildAim.AtScene(hit.point, hit.normal), null, hit.point);
        }
    }
}
