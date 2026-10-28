using System;
using System.Collections.Generic;
using CampfireFriends.Building;
using CampfireFriends.Supplies;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CampfireFriends.Tests
{
    /// <summary>
    /// A self-contained build world for tests: the doc's reference test kit (§6.2), flat ground at
    /// cell layer 0, and a stock of boards. Dispose to destroy the ScriptableObjects it creates.
    /// </summary>
    public sealed class BuildTestKit : IDisposable
    {
        public static readonly Vector3Int BlockSize = new Vector3Int(2, 2, 2);  // 1 × 1 × 1 m
        public static readonly Vector3Int WallSize = new Vector3Int(2, 4, 1);   // 1 × 2 × 0.5 m
        public static readonly Vector3Int RoofSize = new Vector3Int(2, 1, 2);   // 1 × 0.5 × 1 m
        public static readonly Vector3Int CubeSize = new Vector3Int(1, 1, 1);   // 0.5 m cube
        public static readonly Vector3Int LongWallSize = new Vector3Int(8, 4, 1); // 4 × 2 × 0.5 m
        public static readonly Vector3Int BeamSize = new Vector3Int(3, 1, 1);   // 1.5 × 0.5 × 0.5 m

        private readonly List<PieceDefinition> _definitions = new List<PieceDefinition>();

        public readonly BuildGrid Grid = new BuildGrid();
        public readonly BuildWorld World = new BuildWorld();
        public readonly SharedStock Stock = new SharedStock();
        public readonly PlacementValidator Validator;
        public readonly ConstructionService Service;
        public readonly PlacementSolver Solver;

        public BuildTestKit(int boards = 100, IBuildEnvironment environment = null)
        {
            Define("block", BlockSize, SupportRule.Stack, 2);
            Define("wall", WallSize, SupportRule.Stack, 3);
            Define("long_wall", LongWallSize, SupportRule.Stack, 6);
            Define("roof", RoofSize, SupportRule.Roof, 4);
            Define("cube", CubeSize, SupportRule.Stack, 1);
            Define("beam", BeamSize, SupportRule.SideAttach, 2);
            Define("foundation", BlockSize, SupportRule.Ground, 2);
            Define("fixed", CubeSize, SupportRule.Stack, 1).Removable = false;

            Stock.Add(ResourceType.Boards, boards);
            Validator = new PlacementValidator(World, new PieceLibrary(_definitions), environment ?? new FlatGroundEnvironment());
            Service = new ConstructionService(World, Validator, Stock);
            Solver = new PlacementSolver(Grid, World);
        }

        public PieceDefinition Definition(string id) => _definitions.Find(d => d.Id == id);

        public PlacementResult Place(string pieceId, Vector3Int min, GridRotation rotation = GridRotation.R0, int player = 1) =>
            Service.TryPlace(new PlacementRequest { PieceId = pieceId, Min = min, Rotation = rotation, RequestedBy = player });

        public PlacedPiece PlaceOrFail(string pieceId, Vector3Int min, GridRotation rotation = GridRotation.R0)
        {
            var result = Place(pieceId, min, rotation);
            if (!result.Success) throw new InvalidOperationException($"Setup placement of {pieceId} at {min} failed: {result.Rejection}");
            return result.Piece;
        }

        public PlacementRejection Check(string pieceId, Vector3Int min, GridRotation rotation = GridRotation.R0) =>
            Validator.ValidatePlacement(pieceId, min, rotation, Stock);

        /// <summary>Solve for a piece aimed at a world point on a placed piece's face.</summary>
        public PlacementProposal AimAtPiece(string pieceId, PlacedPiece target, Vector3 point, Vector3 normal, GridRotation rotation = GridRotation.R0) =>
            Solver.Solve(Definition(pieceId).Footprint, rotation, BuildAim.AtPiece(target.Id, point, normal));

        public PlacementProposal AimAtGround(string pieceId, Vector3 point, GridRotation rotation = GridRotation.R0) =>
            Solver.Solve(Definition(pieceId).Footprint, rotation, BuildAim.AtScene(point, Vector3.up));

        public void Dispose()
        {
            foreach (var definition in _definitions) Object.DestroyImmediate(definition);
        }

        private PieceDefinition Define(string id, Vector3Int footprint, SupportRule support, int boards)
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>();
            definition.Id = id;
            definition.DisplayName = id;
            definition.Footprint = footprint;
            definition.Support = support;
            definition.Cost = new List<ResourceAmount> { new ResourceAmount(ResourceType.Boards, boards) };
            _definitions.Add(definition);
            return definition;
        }
    }

    /// <summary>Scenery stand-in whose obstruction can be set per test.</summary>
    public sealed class ScriptedEnvironment : IBuildEnvironment
    {
        private readonly FlatGroundEnvironment _ground = new FlatGroundEnvironment();
        public readonly List<CellBounds> Obstacles = new List<CellBounds>();

        public bool HasGroundBelow(Vector3Int cell) => _ground.HasGroundBelow(cell);

        public bool IsObstructed(CellBounds bounds) => Obstacles.Exists(bounds.Overlaps);
    }
}
