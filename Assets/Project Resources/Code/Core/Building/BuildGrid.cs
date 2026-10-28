using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// B01 — the shared 0.5 m lattice. Converts between world space and cell space and gives the
    /// world pose of a piece. Every placed piece is a whole number of cells on this one lattice, so
    /// faces that touch are always exactly flush and always on grid.
    /// </summary>
    public sealed class BuildGrid
    {
        public const float DefaultCellSize = 0.5f;

        public float CellSize { get; }
        public Vector3 Origin { get; }

        public BuildGrid(float cellSize = DefaultCellSize, Vector3 origin = default)
        {
            CellSize = cellSize > 0f ? cellSize : DefaultCellSize;
            Origin = origin;
        }

        /// <summary>Continuous cell coordinates: 1 unit = 1 cell, and cell (0,0,0) spans 0..1 on every axis.</summary>
        public Vector3 ToCellSpace(Vector3 world) => (world - Origin) / CellSize;

        public Vector3 FromCellSpace(Vector3 cellSpace) => Origin + cellSpace * CellSize;

        public Vector3Int WorldToCell(Vector3 world) => Vector3Int.FloorToInt(ToCellSpace(world));

        /// <summary>World position of a cell's minimum corner.</summary>
        public Vector3 CellToWorld(Vector3Int cell) => FromCellSpace(cell);

        public Bounds ToWorldBounds(CellBounds bounds)
        {
            var min = CellToWorld(bounds.Min);
            var size = SizeInMetres(bounds.Size);
            return new Bounds(min + size * 0.5f, size);
        }

        /// <summary>
        /// Where a piece's root goes. Piece prefabs are authored with their pivot at the bottom-centre of
        /// their footprint, so yawing the root about it keeps the footprint on the same cells.
        /// </summary>
        public Vector3 PivotOf(CellBounds bounds)
        {
            var world = ToWorldBounds(bounds);
            return new Vector3(world.center.x, world.min.y, world.center.z);
        }

        public Vector3 SizeInMetres(Vector3Int cells) => (Vector3)cells * CellSize;
    }
}
