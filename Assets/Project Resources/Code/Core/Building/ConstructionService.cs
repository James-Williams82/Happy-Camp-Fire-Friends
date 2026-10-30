using System;
using CampfireFriends.Supplies;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>What a player asks the authority to build. Produced by the local build tool from its preview.</summary>
    public struct PlacementRequest
    {
        public string PieceId;
        public Vector3Int Min;
        public GridRotation Rotation;

        /// <summary>Network player id of the requester; recorded for story/witness use.</summary>
        public int RequestedBy;
    }

    public readonly struct PlacementResult
    {
        public readonly PlacementRejection Rejection;
        public readonly PlacedPiece Piece;

        public bool Success => Rejection == PlacementRejection.None;

        private PlacementResult(PlacementRejection rejection, PlacedPiece piece)
        {
            Rejection = rejection;
            Piece = piece;
        }

        public static PlacementResult Placed(PlacedPiece piece) => new PlacementResult(PlacementRejection.None, piece);
        public static PlacementResult Rejected(PlacementRejection reason) => new PlacementResult(reason, null);
    }

    /// <summary>
    /// B02 — commits construction changes to the authoritative world. Runs on the authority only.
    /// Placing is one transaction (doc §14.4): revalidate → spend once → claim cells → announce.
    /// A rejected request changes nothing and costs nothing.
    /// </summary>
    public sealed class ConstructionService
    {
        private readonly BuildWorld _world;
        private readonly PlacementValidator _validator;
        private readonly SharedStock _stock;
        private int _nextId = 1;

        public event Action<PlacementRequest, PlacementRejection> PlaceRejected;

        public ConstructionService(BuildWorld world, PlacementValidator validator, SharedStock stock)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
        }

        /// <summary>Creative play: pieces cost nothing and removing them refunds nothing. Every other rule still applies.</summary>
        public bool FreeBuilding { get; set; }

        public PlacementResult TryPlace(PlacementRequest request)
        {
            var rejection = _validator.ValidatePlacement(request.PieceId, request.Min, request.Rotation, FreeBuilding ? null : _stock);
            if (rejection != PlacementRejection.None) return Reject(request, rejection);

            _validator.TryGetDefinition(request.PieceId, out var definition);

            // Validation just confirmed affordability, so this cannot fail; checked anyway so a
            // future refactor can't silently build for free.
            if (!FreeBuilding && !_stock.TrySpend(definition.Cost)) return Reject(request, PlacementRejection.InsufficientMaterials);

            var piece = new PlacedPiece(_nextId++, definition.Id, definition.BoundsAt(request.Min, request.Rotation),
                request.Rotation, request.RequestedBy, definition.MaxHealth);
            _world.Add(piece);
            return PlacementResult.Placed(piece);
        }

        public PlacementRejection TryRemove(int placedId)
        {
            var rejection = _validator.ValidateRemoval(placedId);
            if (rejection != PlacementRejection.None) return rejection;

            _world.TryGet(placedId, out var piece);
            _validator.TryGetDefinition(piece.PieceId, out var definition);

            _world.Remove(placedId);
            if (definition.RefundOnRemove && !FreeBuilding) _stock.Add(definition.Cost);
            return PlacementRejection.None;
        }

        /// <summary>
        /// Moves or turns a placed piece, keeping its id, owner and health. Validated first, so a
        /// refused move leaves the world untouched.
        /// </summary>
        public PlacementRejection TryMove(int placedId, Vector3Int min, GridRotation rotation)
        {
            var rejection = _validator.ValidateMove(placedId, min, rotation);
            if (rejection != PlacementRejection.None) return rejection;

            _world.TryGet(placedId, out var piece);
            _validator.TryGetDefinition(piece.PieceId, out var definition);

            _world.Remove(placedId);
            _world.Add(new PlacedPiece(piece.Id, piece.PieceId, definition.BoundsAt(min, rotation), rotation, piece.PlacedBy, piece.Health, piece.On));
            return PlacementRejection.None;
        }

        /// <summary>Uses a placed piece: a toggle piece switches on or off. Free, and only for usable pieces.</summary>
        public PlacementRejection TryUse(int placedId)
        {
            var rejection = _validator.ValidateUse(placedId);
            if (rejection != PlacementRejection.None) return rejection;

            _world.TryGet(placedId, out var piece);
            _world.SetOn(placedId, !piece.On);
            return PlacementRejection.None;
        }

        /// <summary>
        /// Re-inserts a piece from a save without charging materials (X01 restore path). Support is
        /// not rechecked because pieces load in arbitrary order. Unknown ids fail loudly (VAL-11).
        /// </summary>
        public void Restore(PlacedPiece piece)
        {
            if (!_validator.TryGetDefinition(piece.PieceId, out var definition))
                throw new ArgumentException($"Saved piece references unknown piece id '{piece.PieceId}'.");
            if (piece.Bounds.Size != piece.Rotation.RotateSize(definition.Footprint))
                throw new ArgumentException($"Saved piece {piece} does not match the footprint of '{piece.PieceId}'.");

            _world.Add(piece);
            _nextId = Math.Max(_nextId, piece.Id + 1);
        }

        private PlacementResult Reject(PlacementRequest request, PlacementRejection reason)
        {
            PlaceRejected?.Invoke(request, reason);
            return PlacementResult.Rejected(reason);
        }
    }
}
