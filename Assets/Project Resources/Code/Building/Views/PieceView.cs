using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The scene presentation of one piece; it sits on the root of the base piece prefab, which every
    /// piece is a variant of. It is passive: it is told its pose and footprint and shows them.
    /// The target collider is sized from the footprint, so what the pointer hits is always the logical
    /// volume and never the decorative mesh (doc §6.2).
    /// </summary>
    [DisallowMultipleComponent]
    public class PieceView : MonoBehaviour
    {
        [Tooltip("Logical bounds collider the build pointer targets. Sized from the footprint at runtime.")]
        [SerializeField] private BoxCollider _bounds;

        [Tooltip("Parent of the decorative meshes. Only these renderers are tinted.")]
        [SerializeField] private Transform _visual;

        private Renderer[] _renderers;
        private Material[][] _originalMaterials;

        /// <summary>The placed piece this view shows, or <see cref="BuildWorld.NoPiece"/> for previews.</summary>
        public int PlacedId { get; private set; } = BuildWorld.NoPiece;

        public bool IsPlaced => PlacedId != BuildWorld.NoPiece;

        public static bool IsPartOfPiece(Collider collider) => collider != null && collider.GetComponentInParent<PieceView>() != null;

        public void Present(PieceDefinition definition, BuildGrid grid, CellBounds bounds, GridRotation rotation)
        {
            transform.SetPositionAndRotation(grid.PivotOf(bounds), rotation.ToQuaternion());
            FitBounds(grid.SizeInMetres(definition.Footprint));
        }

        public void BindPlaced(PlacedPiece piece, PieceDefinition definition, BuildGrid grid)
        {
            PlacedId = piece.Id;
            name = $"{definition.Id} #{piece.Id}";
            Present(definition, grid, piece.Bounds, piece.Rotation);
        }

        /// <summary>Turns this instance into a ghost: nothing can hit it and it never blocks anything.</summary>
        public void MakeIntangible()
        {
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
            PlacedId = BuildWorld.NoPiece;
        }

        /// <summary>Draws every visual renderer with <paramref name="material"/> until <see cref="ClearMaterialOverride"/>.</summary>
        public void OverrideMaterial(Material material)
        {
            CacheRenderers();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var slots = new Material[_originalMaterials[i].Length];
                for (int s = 0; s < slots.Length; s++) slots[s] = material;
                _renderers[i].sharedMaterials = slots;
            }
        }

        public void ClearMaterialOverride()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].sharedMaterials = _originalMaterials[i];
            }
        }

        private void FitBounds(Vector3 size)
        {
            if (_bounds == null) return;
            _bounds.size = size;
            _bounds.center = new Vector3(0f, size.y * 0.5f, 0f);
        }

        private void CacheRenderers()
        {
            if (_renderers != null) return;
            _renderers = (_visual != null ? _visual : transform).GetComponentsInChildren<Renderer>(true);
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++) _originalMaterials[i] = _renderers[i].sharedMaterials;
        }

        private void Reset()
        {
            _bounds = GetComponentInChildren<BoxCollider>();
            var visual = transform.Find("Visual");
            _visual = visual != null ? visual : transform;
        }
    }
}
