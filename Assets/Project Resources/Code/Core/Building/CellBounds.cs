using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// An axis-aligned box of whole grid cells: the logical volume a piece occupies.
    /// <see cref="Max"/> is exclusive, so two boxes whose faces touch share no cells.
    /// </summary>
    public readonly struct CellBounds : IEquatable<CellBounds>
    {
        public readonly Vector3Int Min;
        public readonly Vector3Int Size;

        public CellBounds(Vector3Int min, Vector3Int size)
        {
            Min = min;
            Size = Vector3Int.Max(size, Vector3Int.one);
        }

        public static CellBounds ForPiece(Vector3Int min, Vector3Int footprint, GridRotation rotation) =>
            new CellBounds(min, rotation.RotateSize(footprint));

        public Vector3Int Max => Min + Size;
        public int Volume => Size.x * Size.y * Size.z;

        public bool Contains(Vector3Int cell) =>
            cell.x >= Min.x && cell.x < Max.x &&
            cell.y >= Min.y && cell.y < Max.y &&
            cell.z >= Min.z && cell.z < Max.z;

        public bool Overlaps(CellBounds other) =>
            Min.x < other.Max.x && other.Min.x < Max.x &&
            Min.y < other.Max.y && other.Min.y < Max.y &&
            Min.z < other.Max.z && other.Min.z < Max.z;

        public IEnumerable<Vector3Int> Cells()
        {
            for (int x = Min.x; x < Max.x; x++)
            for (int y = Min.y; y < Max.y; y++)
            for (int z = Min.z; z < Max.z; z++)
                yield return new Vector3Int(x, y, z);
        }

        /// <summary>The cells of the lowest layer — the ones that need something underneath.</summary>
        public IEnumerable<Vector3Int> BottomCells()
        {
            for (int x = Min.x; x < Max.x; x++)
            for (int z = Min.z; z < Max.z; z++)
                yield return new Vector3Int(x, Min.y, z);
        }

        /// <summary>Cells outside the box that share a face with it: above, below and on all four sides.</summary>
        public IEnumerable<Vector3Int> FaceNeighbours()
        {
            for (int x = Min.x; x < Max.x; x++)
            for (int z = Min.z; z < Max.z; z++)
            {
                yield return new Vector3Int(x, Min.y - 1, z);
                yield return new Vector3Int(x, Max.y, z);
            }
            foreach (var cell in SideNeighbours(Min.y, Max.y)) yield return cell;
        }

        /// <summary>Cells beside the box on its four horizontal sides, for layers <paramref name="fromY"/> up to (not including) <paramref name="toY"/>.</summary>
        public IEnumerable<Vector3Int> SideNeighbours(int fromY, int toY)
        {
            for (int y = fromY; y < toY; y++)
            {
                for (int x = Min.x; x < Max.x; x++)
                {
                    yield return new Vector3Int(x, y, Min.z - 1);
                    yield return new Vector3Int(x, y, Max.z);
                }
                for (int z = Min.z; z < Max.z; z++)
                {
                    yield return new Vector3Int(Min.x - 1, y, z);
                    yield return new Vector3Int(Max.x, y, z);
                }
            }
        }

        public bool Equals(CellBounds other) => Min == other.Min && Size == other.Size;
        public override bool Equals(object obj) => obj is CellBounds other && Equals(other);
        public override int GetHashCode() => (Min.GetHashCode() * 397) ^ Size.GetHashCode();
        public static bool operator ==(CellBounds a, CellBounds b) => a.Equals(b);
        public static bool operator !=(CellBounds a, CellBounds b) => !a.Equals(b);

        public override string ToString() => $"[{Min} .. {Max})";
    }
}
