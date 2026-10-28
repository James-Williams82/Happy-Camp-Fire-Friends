using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>Yaw in 90° steps — the only rotations the build tool allows.</summary>
    public enum GridRotation
    {
        R0 = 0,
        R90 = 1,
        R180 = 2,
        R270 = 3,
    }

    public static class GridRotationExtensions
    {
        public static GridRotation Rotated(this GridRotation rotation, int quarterTurns) =>
            (GridRotation)((((int)rotation + quarterTurns) % 4 + 4) % 4);

        public static int Degrees(this GridRotation rotation) => 90 * (int)rotation;

        public static Quaternion ToQuaternion(this GridRotation rotation) => Quaternion.Euler(0f, rotation.Degrees(), 0f);

        /// <summary>A footprint's world-axis size after yawing: quarter and three-quarter turns swap width and depth.</summary>
        public static Vector3Int RotateSize(this GridRotation rotation, Vector3Int size) =>
            (int)rotation % 2 == 0 ? size : new Vector3Int(size.z, size.y, size.x);
    }
}
