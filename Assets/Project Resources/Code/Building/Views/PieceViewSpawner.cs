using System.Collections.Generic;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Keeps one <see cref="PieceView"/> per piece in a <see cref="BuildWorld"/>: spawns on Added,
    /// destroys on Removed. Used where nothing else owns the views (the offline sandbox).
    /// </summary>
    public sealed class PieceViewSpawner
    {
        private readonly BuildWorld _world;
        private readonly IPieceLookup _pieces;
        private readonly BuildGrid _grid;
        private readonly Transform _parent;
        private readonly Dictionary<int, PieceView> _views = new Dictionary<int, PieceView>();

        public PieceViewSpawner(BuildWorld world, IPieceLookup pieces, BuildGrid grid, Transform parent)
        {
            _world = world;
            _pieces = pieces;
            _grid = grid;
            _parent = parent;

            _world.Added += Spawn;
            _world.Removed += Despawn;
        }

        public void Dispose()
        {
            _world.Added -= Spawn;
            _world.Removed -= Despawn;
            foreach (var view in _views.Values)
            {
                if (view != null) Object.Destroy(view.gameObject);
            }
            _views.Clear();
        }

        public static PieceView Create(PieceDefinition definition, Transform parent)
        {
            if (definition.Prefab == null)
            {
                Debug.LogError($"[Construction] Piece '{definition.Id}' has no prefab.", definition);
                return null;
            }

            var instance = Object.Instantiate(definition.Prefab, parent);
            var view = instance.GetComponent<PieceView>();
            if (view == null)
            {
                Debug.LogError($"[Construction] Prefab for '{definition.Id}' is not a variant of the base piece (no PieceView).", definition.Prefab);
                Object.Destroy(instance);
            }
            return view;
        }

        private void Spawn(PlacedPiece piece)
        {
            if (!_pieces.TryGet(piece.PieceId, out var definition)) return;

            var view = Create(definition, _parent);
            if (view == null) return;

            view.BindPlaced(piece, definition, _grid);
            _views[piece.Id] = view;
        }

        private void Despawn(PlacedPiece piece)
        {
            if (!_views.TryGetValue(piece.Id, out var view)) return;
            _views.Remove(piece.Id);
            if (view != null) Object.Destroy(view.gameObject);
        }
    }
}
