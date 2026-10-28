using System;
using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// An offline construction session: this machine is the authority. Used by the build sandbox so
    /// placement can be tested with Play alone, no network session. It composes the same Core services
    /// the host runs, so behaviour here is behaviour online.
    /// </summary>
    public class LocalConstructionBackend : MonoBehaviour, IConstructionBackend
    {
        [SerializeField] private PieceCatalog _catalog;
        [SerializeField] private List<ResourceAmount> _startingStock = new List<ResourceAmount>();
        [SerializeField] private PhysicsEnvironmentSettings _environment = new PhysicsEnvironmentSettings();

        [Tooltip("Parent for placed piece views. Defaults to this object.")]
        [SerializeField] private Transform _piecesRoot;

        private SharedStock _stock;
        private ConstructionService _service;
        private PieceViewSpawner _views;

        public PieceCatalog Catalog => _catalog;
        public BuildGrid Grid { get; private set; }
        public BuildWorld World { get; private set; }
        public PlacementValidator Validator { get; private set; }
        public IStockView Stock => _stock;

        public event Action<PlacementRejection> RequestRejected;

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError("[Construction] No piece catalog assigned; building is off. Run Campfire Friends/Building/Rebuild Build Kit.", this);
                enabled = false;
                return;
            }

            var pieces = _catalog.CreateLibrary();
            Grid = _catalog.CreateGrid();
            World = new BuildWorld();
            Validator = new PlacementValidator(World, pieces, new PhysicsBuildEnvironment(Grid, _environment));

            _stock = new SharedStock();
            _stock.Add(_startingStock);
            _service = new ConstructionService(World, Validator, _stock);
            _views = new PieceViewSpawner(World, pieces, Grid, _piecesRoot != null ? _piecesRoot : transform);
        }

        private void OnEnable() => ConstructionBackends.Register(this);
        private void OnDisable() => ConstructionBackends.Unregister(this);
        private void OnDestroy() => _views?.Dispose();

        public void RequestPlace(string pieceId, Vector3Int min, GridRotation rotation)
        {
            var result = _service.TryPlace(new PlacementRequest { PieceId = pieceId, Min = min, Rotation = rotation });
            if (!result.Success) RequestRejected?.Invoke(result.Rejection);
        }

        public void RequestRemove(int placedId)
        {
            var rejection = _service.TryRemove(placedId);
            if (rejection != PlacementRejection.None) RequestRejected?.Invoke(rejection);
        }

        public void RequestMove(int placedId, Vector3Int min, GridRotation rotation)
        {
            var rejection = _service.TryMove(placedId, min, rotation);
            if (rejection != PlacementRejection.None) RequestRejected?.Invoke(rejection);
        }
    }
}
