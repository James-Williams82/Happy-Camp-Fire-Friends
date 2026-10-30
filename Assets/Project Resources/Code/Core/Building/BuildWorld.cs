using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// The registry of placed pieces and the cells they claim (doc §15.2). The host's copy is
    /// authoritative; clients keep a mirror fed by replication so their previews see the same world.
    /// It enforces one invariant: no cell is ever claimed by two pieces.
    /// </summary>
    public sealed class BuildWorld
    {
        public const int NoPiece = 0;

        private readonly Dictionary<Vector3Int, int> _cells = new Dictionary<Vector3Int, int>();
        private readonly Dictionary<int, PlacedPiece> _pieces = new Dictionary<int, PlacedPiece>();

        public event Action<PlacedPiece> Added;
        public event Action<PlacedPiece> Removed;

        /// <summary>A placed piece's state changed in place (switched on or off); it didn't move.</summary>
        public event Action<PlacedPiece> StateChanged;

        public IEnumerable<PlacedPiece> Pieces => _pieces.Values;
        public int Count => _pieces.Count;

        public bool TryGet(int id, out PlacedPiece piece) => _pieces.TryGetValue(id, out piece);

        public bool Contains(int id) => _pieces.ContainsKey(id);

        /// <summary>The id of the piece claiming <paramref name="cell"/>, or <see cref="NoPiece"/>. Pieces in <paramref name="ignore"/> are treated as absent.</summary>
        public int PieceAt(Vector3Int cell, int ignore = NoPiece)
        {
            return _cells.TryGetValue(cell, out var id) && id != ignore ? id : NoPiece;
        }

        public bool IsFree(CellBounds bounds, int ignore = NoPiece)
        {
            foreach (var cell in bounds.Cells())
            {
                if (PieceAt(cell, ignore) != NoPiece) return false;
            }
            return true;
        }

        public void Add(PlacedPiece piece)
        {
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            if (piece.Id <= NoPiece) throw new ArgumentException($"Piece ids must be positive, got {piece.Id}.");
            if (_pieces.ContainsKey(piece.Id)) throw new InvalidOperationException($"Piece {piece.Id} is already in the world.");

            foreach (var cell in piece.Bounds.Cells())
            {
                if (_cells.TryGetValue(cell, out var other))
                    throw new InvalidOperationException($"{piece} overlaps piece {other} at {cell}.");
            }

            foreach (var cell in piece.Bounds.Cells()) _cells[cell] = piece.Id;
            _pieces.Add(piece.Id, piece);
            Added?.Invoke(piece);
        }

        public bool Remove(int id)
        {
            if (!_pieces.TryGetValue(id, out var piece)) return false;

            foreach (var cell in piece.Bounds.Cells()) _cells.Remove(cell);
            _pieces.Remove(id);
            Removed?.Invoke(piece);
            return true;
        }

        /// <summary>Switches a piece on or off. Returns false for an unknown piece; no event if nothing changes.</summary>
        public bool SetOn(int id, bool on)
        {
            if (!_pieces.TryGetValue(id, out var piece)) return false;
            if (piece.On == on) return true;

            piece.On = on;
            StateChanged?.Invoke(piece);
            return true;
        }

        public void Clear()
        {
            foreach (var id in new List<int>(_pieces.Keys)) Remove(id);
        }
    }
}
