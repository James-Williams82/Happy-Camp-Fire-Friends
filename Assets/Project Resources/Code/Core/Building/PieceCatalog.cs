using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>Looks up piece definitions by their durable id.</summary>
    public interface IPieceLookup
    {
        bool TryGet(string pieceId, out PieceDefinition definition);
    }

    /// <summary>An id → definition index that rejects missing and duplicate ids up front.</summary>
    public sealed class PieceLibrary : IPieceLookup
    {
        private readonly Dictionary<string, PieceDefinition> _byId = new Dictionary<string, PieceDefinition>();

        public PieceLibrary(IEnumerable<PieceDefinition> pieces)
        {
            foreach (var piece in pieces)
            {
                if (piece == null || string.IsNullOrEmpty(piece.Id))
                    throw new ArgumentException("Every piece needs an Id.");
                if (_byId.ContainsKey(piece.Id))
                    throw new ArgumentException($"Duplicate piece id '{piece.Id}'.");
                _byId.Add(piece.Id, piece);
            }
        }

        public bool TryGet(string pieceId, out PieceDefinition definition) =>
            _byId.TryGetValue(pieceId ?? string.Empty, out definition);
    }

    /// <summary>
    /// The ordered list of buildable pieces for a session. Network messages refer to pieces by
    /// index into this list, so every peer must use the same catalog asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Building/Piece Catalog", fileName = "PieceCatalog")]
    public class PieceCatalog : ScriptableObject
    {
        [Tooltip("Logical grid cell size in metres.")]
        public float CellSize = BuildGrid.DefaultCellSize;

        public List<PieceDefinition> Pieces = new List<PieceDefinition>();

        public int Count => Pieces.Count;

        public BuildGrid CreateGrid() => new BuildGrid(CellSize);

        public PieceLibrary CreateLibrary() => new PieceLibrary(Pieces);

        public int IndexOf(string pieceId) => Pieces.FindIndex(p => p != null && p.Id == pieceId);

        public PieceDefinition Get(int index) => index >= 0 && index < Pieces.Count ? Pieces[index] : null;
    }
}
