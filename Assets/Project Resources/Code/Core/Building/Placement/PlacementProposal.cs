using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>How the solver settled one axis of a proposal.</summary>
    public enum Alignment
    {
        /// <summary>Centred on the aim and rounded to the lattice.</summary>
        Free,

        /// <summary>Flush against the face that was hit.</summary>
        Contact,

        /// <summary>Low ends lined up with the target.</summary>
        Min,

        /// <summary>Centres lined up with the target.</summary>
        Center,

        /// <summary>High ends lined up with the target.</summary>
        Max,
    }

    /// <summary>
    /// The solver's answer: the exact pose to preview and request, plus how it was reached so a
    /// debugger can see why a piece went where it did (doc §19.4).
    /// </summary>
    public readonly struct PlacementProposal
    {
        public readonly bool HasPose;
        public readonly CellBounds Bounds;
        public readonly GridRotation Rotation;

        /// <summary>Outward direction of the face built against (up for ground and top faces).</summary>
        public readonly Vector3Int Face;

        public readonly Alignment AlignX;
        public readonly Alignment AlignY;
        public readonly Alignment AlignZ;

        public PlacementProposal(CellBounds bounds, GridRotation rotation, Vector3Int face, Alignment x, Alignment y, Alignment z)
        {
            HasPose = true;
            Bounds = bounds;
            Rotation = rotation;
            Face = face;
            AlignX = x;
            AlignY = y;
            AlignZ = z;
        }

        public static PlacementProposal None => default;

        public Vector3Int Min => Bounds.Min;

        public override string ToString() =>
            HasPose ? $"{Bounds} {Rotation} face {Face} align ({AlignX}, {AlignY}, {AlignZ})" : "no pose";
    }
}
