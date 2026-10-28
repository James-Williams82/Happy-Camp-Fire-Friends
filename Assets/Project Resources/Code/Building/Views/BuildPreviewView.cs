using System.Collections.Generic;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The local-only ghost of the piece being placed (doc §13.2). It reuses each piece's own prefab
    /// variant, made intangible and drawn with the valid or invalid material, so the ghost is always
    /// exactly the shape that will be built. Ghosts are cached per piece.
    /// </summary>
    public class BuildPreviewView : MonoBehaviour
    {
        [SerializeField] private Material _validMaterial;
        [SerializeField] private Material _invalidMaterial;

        [Tooltip("Optional outline of the logical footprint, for seeing exactly which cells are claimed.")]
        [SerializeField] private BoundsOutlineView _outline;

        private readonly Dictionary<PieceDefinition, PieceView> _ghosts = new Dictionary<PieceDefinition, PieceView>();
        private PieceView _shown;

        public void Show(PieceDefinition definition, BuildGrid grid, CellBounds bounds, GridRotation rotation, bool valid)
        {
            var ghost = GhostFor(definition);
            if (ghost == null) return;

            if (_shown != ghost) Hide();
            _shown = ghost;

            ghost.Present(definition, grid, bounds, rotation);
            ghost.OverrideMaterial(valid ? _validMaterial : _invalidMaterial);
            ghost.gameObject.SetActive(true);

            if (_outline != null) _outline.Show(grid.ToWorldBounds(bounds), valid);
        }

        public void Hide()
        {
            if (_shown != null) _shown.gameObject.SetActive(false);
            _shown = null;
            if (_outline != null) _outline.Hide();
        }

        private PieceView GhostFor(PieceDefinition definition)
        {
            if (_ghosts.TryGetValue(definition, out var ghost) && ghost != null) return ghost;

            ghost = PieceViewSpawner.Create(definition, transform);
            if (ghost == null) return null;

            ghost.name = $"Ghost {definition.Id}";
            ghost.MakeIntangible();
            ghost.gameObject.SetActive(false);
            _ghosts[definition] = ghost;
            return ghost;
        }
    }
}
