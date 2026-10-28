using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Construction;
using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// The networked stand-in for one committed piece. A single Fusion prefab serves every piece type:
    /// it replicates the piece's durable data, builds its <see cref="PieceView"/> from the catalog, and
    /// feeds the client's mirror world. Pieces don't move, so the pose is derived from the grid
    /// instead of being synced.
    /// </summary>
    public class PlacedPieceProxy : NetworkBehaviour
    {
        private static readonly List<PlacedPieceProxy> ActiveProxies = new List<PlacedPieceProxy>();

        [SerializeField] private PieceCatalog _catalog;

        [Networked] public int PlacedId { get; set; }
        [Networked] public int PieceIndex { get; set; }
        [Networked] public Vector3Int Min { get; set; }
        [Networked] public byte Rotation { get; set; }
        [Networked] public int PlacedBy { get; set; }
        [Networked] public int Health { get; set; }

        private PieceView _view;

        // Networked state may be gone by Despawned, so remember which piece this was.
        private int _spawnedId;

        public static IReadOnlyList<PlacedPieceProxy> Active => ActiveProxies;

        public PieceDefinition Definition => _catalog.Get(PieceIndex);

        /// <summary>Called by the host before the spawn is replicated.</summary>
        public void Init(PlacedPiece piece, int pieceIndex)
        {
            PlacedId = piece.Id;
            PieceIndex = pieceIndex;
            Min = piece.Bounds.Min;
            Rotation = (byte)piece.Rotation;
            PlacedBy = piece.PlacedBy;
            Health = piece.Health;
        }

        public PlacedPiece ToPlacedPiece()
        {
            var definition = Definition;
            if (definition == null) return null;

            var rotation = (GridRotation)(Rotation & 3);
            return new PlacedPiece(PlacedId, definition.Id, definition.BoundsAt(Min, rotation), rotation, PlacedBy, Health);
        }

        public override void Spawned()
        {
            ActiveProxies.Add(this);
            _spawnedId = PlacedId;

            var piece = ToPlacedPiece();
            if (piece == null)
            {
                Debug.LogError($"[Construction] Placed piece {PlacedId} has unknown catalog index {PieceIndex}.", this);
                return;
            }

            var grid = _catalog.CreateGrid();
            transform.SetPositionAndRotation(grid.PivotOf(piece.Bounds), piece.Rotation.ToQuaternion());
            name = $"{piece.PieceId} #{PlacedId}";

            _view = PieceViewSpawner.Create(Definition, transform);
            if (_view != null) _view.BindPlaced(piece, Definition, grid);

            if (ConstructionAuthority.Instance != null) ConstructionAuthority.Instance.Mirror(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ActiveProxies.Remove(this);
            if (ConstructionAuthority.Instance != null) ConstructionAuthority.Instance.Unmirror(_spawnedId);
        }
    }
}
