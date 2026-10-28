using System;
using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Construction;
using CampfireFriends.Supplies;
using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// The networked construction session; one lives in each playable scene as a scene NetworkObject.
    /// The host owns the authoritative world, stock and <see cref="ConstructionService"/>. Clients
    /// never mutate anything: they send requests over RPC, and the results come back as piece proxies
    /// (which feed each client's mirror world) and replicated stock values.
    /// Every peer gets the same validator over its world, so local previews agree with the host.
    /// </summary>
    public class ConstructionAuthority : NetworkBehaviour, IConstructionBackend
    {
        public static ConstructionAuthority Instance { get; private set; }

        [SerializeField] private PieceCatalog _catalog;
        [SerializeField] private NetworkObject _placedPiecePrefab;
        [SerializeField] private List<ResourceAmount> _startingStock = new List<ResourceAmount>();
        [SerializeField] private PhysicsEnvironmentSettings _environment = new PhysicsEnvironmentSettings();

        [Networked, Capacity(8)] private NetworkArray<int> StockAmounts => default;

        private SharedStock _hostStock;
        private ConstructionService _service;
        private IStockView _replicatedStock;
        private readonly Dictionary<int, NetworkObject> _proxies = new Dictionary<int, NetworkObject>();

        public PieceCatalog Catalog => _catalog;
        public BuildGrid Grid { get; private set; }
        public BuildWorld World { get; private set; }
        public PlacementValidator Validator { get; private set; }
        public IStockView Stock => _hostStock != null ? _hostStock : _replicatedStock;

        /// <summary>The authoritative stock. Null on clients.</summary>
        public SharedStock HostStock => _hostStock;

        public event Action<PlacementRejection> RequestRejected;

        public override void Spawned()
        {
            Instance = this;

            var pieces = _catalog.CreateLibrary();
            Grid = _catalog.CreateGrid();
            World = new BuildWorld();
            Validator = new PlacementValidator(World, pieces, new PhysicsBuildEnvironment(Grid, _environment));
            _replicatedStock = new ReplicatedStock(this);

            if (HasStateAuthority)
            {
                _hostStock = new SharedStock();
                _hostStock.StockChanged += (type, amount) => StockAmounts.Set((int)type, amount);
                _hostStock.Add(_startingStock);

                _service = new ConstructionService(World, Validator, _hostStock);
                World.Added += SpawnProxy;
                World.Removed += DespawnProxy;
            }
            else
            {
                // Late joiners: proxies that spawned before this object did.
                foreach (var proxy in PlacedPieceProxy.Active) Mirror(proxy);
            }

            ConstructionBackends.Register(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ConstructionBackends.Unregister(this);
            if (Instance == this) Instance = null;
        }

        public void RequestPlace(string pieceId, Vector3Int min, GridRotation rotation) =>
            RPC_RequestPlace(_catalog.IndexOf(pieceId), min, (byte)rotation);

        public void RequestRemove(int placedId) => RPC_RequestRemove(placedId);

        /// <summary>Client side: a piece proxy arrived, so add it to the mirror world.</summary>
        internal void Mirror(PlacedPieceProxy proxy)
        {
            if (HasStateAuthority || World == null || World.Contains(proxy.PlacedId)) return;

            var piece = proxy.ToPlacedPiece();
            if (piece != null) World.Add(piece);
        }

        /// <summary>Client side: a piece proxy left, so drop it from the mirror world.</summary>
        internal void Unmirror(int placedId)
        {
            if (!HasStateAuthority && World != null) World.Remove(placedId);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestPlace(int pieceIndex, Vector3Int min, byte rotation, RpcInfo info = default)
        {
            var source = SourceOf(info);
            var piece = _catalog.Get(pieceIndex);
            var result = _service.TryPlace(new PlacementRequest
            {
                PieceId = piece != null ? piece.Id : null,
                Min = min,
                Rotation = (GridRotation)(rotation & 3),
                RequestedBy = source.PlayerId,
            });

            if (!result.Success) RPC_RequestRejected(source, (int)result.Rejection);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestRemove(int placedId, RpcInfo info = default)
        {
            var rejection = _service.TryRemove(placedId);
            if (rejection != PlacementRejection.None) RPC_RequestRejected(SourceOf(info), (int)rejection);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_RequestRejected([RpcTarget] PlayerRef player, int rejection)
        {
            RequestRejected?.Invoke((PlacementRejection)rejection);
        }

        private PlayerRef SourceOf(RpcInfo info) => info.IsInvokeLocal ? Runner.LocalPlayer : info.Source;

        private void SpawnProxy(PlacedPiece piece)
        {
            int pieceIndex = _catalog.IndexOf(piece.PieceId);
            var spawned = Runner.Spawn(
                _placedPiecePrefab,
                Grid.PivotOf(piece.Bounds),
                piece.Rotation.ToQuaternion(),
                onBeforeSpawned: (runner, obj) => obj.GetComponent<PlacedPieceProxy>().Init(piece, pieceIndex));

            _proxies[piece.Id] = spawned;
        }

        private void DespawnProxy(PlacedPiece piece)
        {
            if (!_proxies.TryGetValue(piece.Id, out var spawned)) return;
            _proxies.Remove(piece.Id);
            if (spawned != null) Runner.Despawn(spawned);
        }

        private sealed class ReplicatedStock : IStockView
        {
            private readonly ConstructionAuthority _owner;

            public ReplicatedStock(ConstructionAuthority owner) => _owner = owner;

            public int Get(ResourceType type) => _owner.StockAmounts[(int)type];
        }
    }
}
