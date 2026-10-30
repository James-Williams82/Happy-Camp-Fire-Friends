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
    /// instead of being synced. State that changes in place (a light switched on) is synced and
    /// shown as it arrives.
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
        [Networked] public NetworkBool On { get; set; }

        private PieceView _view;

        // Networked state may be gone by Despawned, so remember which piece this was.
        private int _spawnedId;

        // The state the view and mirror last showed, to notice replicated changes.
        private bool _shownOn;

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
            On = piece.On;
        }

        public PlacedPiece ToPlacedPiece()
        {
            var definition = Definition;
            if (definition == null) return null;

            var rotation = (GridRotation)(Rotation & 3);
            return new PlacedPiece(PlacedId, definition.Id, definition.BoundsAt(Min, rotation), rotation, PlacedBy, Health, On);
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
            _shownOn = piece.On;

            if (ConstructionAuthority.Instance != null) ConstructionAuthority.Instance.Mirror(this);
        }

        public override void Render()
        {
            if (On == _shownOn) return;
            _shownOn = On;

            if (_view != null) _view.ShowState(_shownOn);
            if (ConstructionAuthority.Instance != null) ConstructionAuthority.Instance.MirrorState(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ActiveProxies.Remove(this);
            if (ConstructionAuthority.Instance != null) ConstructionAuthority.Instance.Unmirror(_spawnedId, this);
        }
    }
}
