using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Shows a piece joined up with its neighbours: one part per side (say, a short fence board),
    /// shown on each side where a piece of the same connect group meets it end-on. It follows the
    /// world, so it rejoins as fences around it are placed, moved and removed, on every machine.
    /// Previews show no joins.
    /// </summary>
    public class PieceConnectionView : MonoBehaviour
    {
        [SerializeField] private PieceView _piece;

        [Header("Parts per side, in the prefab's own axes")]
        [SerializeField] private GameObject _posX;
        [SerializeField] private GameObject _negX;
        [SerializeField] private GameObject _posZ;
        [SerializeField] private GameObject _negZ;

        private IConstructionBackend _backend;

        private void OnDisable() => Bind(null);

        private void Update()
        {
            var backend = _piece != null && _piece.IsPlaced ? ConstructionBackends.Current : null;
            if (backend != _backend) Bind(backend);
        }

        private void Bind(IConstructionBackend backend)
        {
            if (_backend != null)
            {
                _backend.World.Added -= OnWorldChanged;
                _backend.World.Removed -= OnWorldChanged;
            }

            _backend = backend;
            if (_backend != null)
            {
                _backend.World.Added += OnWorldChanged;
                _backend.World.Removed += OnWorldChanged;
            }
            Refresh();
        }

        private void OnWorldChanged(PlacedPiece changed) => Refresh();

        private void Refresh()
        {
            var sides = ConnectedSides.None;
            if (_backend != null && _backend.World.TryGet(_piece.PlacedId, out var placed))
                sides = PieceConnections.Of(_backend.World, new ValidatorLookup(_backend.Validator), placed);

            bool posX = false, negX = false, posZ = false, negZ = false;
            foreach (var side in new[] { ConnectedSides.PosX, ConnectedSides.NegX, ConnectedSides.PosZ, ConnectedSides.NegZ })
            {
                if ((sides & side) == 0) continue;

                // World side → the prefab's own side, through the piece's grid rotation.
                var local = Quaternion.Inverse(transform.rotation) * (Vector3)PieceConnections.Direction(side);
                if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
                {
                    if (local.x > 0f) posX = true;
                    else negX = true;
                }
                else if (local.z > 0f) posZ = true;
                else negZ = true;
            }

            Show(_posX, posX);
            Show(_negX, negX);
            Show(_posZ, posZ);
            Show(_negZ, negZ);
        }

        private static void Show(GameObject part, bool visible)
        {
            if (part != null && part.activeSelf != visible) part.SetActive(visible);
        }

        /// <summary>Reads definitions through the session's validator, which already holds the catalog.</summary>
        private readonly struct ValidatorLookup : IPieceLookup
        {
            private readonly PlacementValidator _validator;

            public ValidatorLookup(PlacementValidator validator) => _validator = validator;

            public bool TryGet(string pieceId, out PieceDefinition definition) => _validator.TryGetDefinition(pieceId, out definition);
        }
    }
}
