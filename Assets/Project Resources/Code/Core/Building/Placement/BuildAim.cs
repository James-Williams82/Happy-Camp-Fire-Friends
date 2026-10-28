using UnityEngine;

namespace CampfireFriends.Building
{
    public enum AimSurface
    {
        /// <summary>The pointer hits nothing in range.</summary>
        None,

        /// <summary>Static scenery: ground when the face points up, otherwise a wall or obstacle.</summary>
        Scene,

        /// <summary>A placed piece; <see cref="BuildAim.PieceId"/> says which.</summary>
        Piece,
    }

    /// <summary>What the player is pointing at: the raw intent the solver turns into an exact pose.</summary>
    public readonly struct BuildAim
    {
        public readonly AimSurface Surface;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly int PieceId;

        public BuildAim(AimSurface surface, Vector3 point, Vector3 normal, int pieceId = BuildWorld.NoPiece)
        {
            Surface = surface;
            Point = point;
            Normal = normal;
            PieceId = pieceId;
        }

        public static BuildAim Nothing => new BuildAim(AimSurface.None, Vector3.zero, Vector3.up);

        public static BuildAim AtScene(Vector3 point, Vector3 normal) => new BuildAim(AimSurface.Scene, point, normal);

        public static BuildAim AtPiece(int pieceId, Vector3 point, Vector3 normal) => new BuildAim(AimSurface.Piece, point, normal, pieceId);
    }
}
