using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>
    /// Turns rough aim into an exact pose — "the player expresses intent, not centimetre accuracy" (doc §2.1).
    /// It only answers where a piece should go; whether it may go there is <see cref="PlacementValidator"/>'s job.
    ///
    /// Everything is solved in whole cells on the shared lattice, which makes the doc's two snaps
    /// (face contact and lattice quantisation, §9.6) the same operation: a flush face is always on
    /// grid and quantising can never open a seam or push a piece into its neighbour.
    ///
    /// Against a placed piece (§9.1):
    ///   1. The dominant axis of the hit normal picks the face; the new piece goes flush against it.
    ///   2. Each other axis, vertical included, lines up with the target's low edge, centre or high
    ///      edge — whichever line is nearest the aim within the magnet distance, preferring the band
    ///      (low/middle/high third) the player is pointing at. Nothing in range means a free lattice
    ///      position centred on the aim, so small pieces can still slide along long faces.
    ///   3. Side faces have edge bands. Aiming just under the top edge places the piece on top of the
    ///      target (stacking without having to see the top face); just inside a side edge extends the
    ///      row beside it. Either way it stays flush with the face being aimed at. The nearest edge
    ///      wins, top on a tie; the bottom edge and the middle of the face keep face placement.
    /// On ground the piece sits on the nearest lattice level, centred on the aim.
    /// </summary>
    public sealed class PlacementSolver
    {
        private const float PlaneEpsilon = 0.001f;

        private enum FaceEdge
        {
            None,
            Top,
            TangentLow,
            TangentHigh,
        }

        private readonly BuildGrid _grid;
        private readonly BuildWorld _world;
        private readonly SolverSettings _settings;

        public PlacementSolver(BuildGrid grid, BuildWorld world, SolverSettings settings = null)
        {
            _grid = grid;
            _world = world;
            _settings = settings ?? new SolverSettings();
        }

        public PlacementProposal Solve(Vector3Int footprint, GridRotation rotation, BuildAim aim)
        {
            var size = rotation.RotateSize(Vector3Int.Max(footprint, Vector3Int.one));
            var p = _grid.ToCellSpace(aim.Point);

            switch (aim.Surface)
            {
                case AimSurface.Piece when _world.TryGet(aim.PieceId, out var target):
                    return AgainstPiece(target.Bounds, size, rotation, p, aim.Normal);

                case AimSurface.Piece:
                case AimSurface.Scene:
                    return aim.Normal.y >= _settings.GroundNormalMinY
                        ? OnGround(size, rotation, p)
                        : AgainstScenery(size, rotation, p, aim.Normal);

                default:
                    return PlacementProposal.None;
            }
        }

        private static PlacementProposal OnGround(Vector3Int size, GridRotation rotation, Vector3 p)
        {
            var min = new Vector3Int(Centred(p.x, size.x), RoundHalfUp(p.y), Centred(p.z, size.z));
            return new PlacementProposal(new CellBounds(min, size), rotation, Vector3Int.up,
                Alignment.Free, Alignment.Contact, Alignment.Free);
        }

        /// <summary>
        /// Walls of the camp aren't on the lattice, so the contact axis moves outward to the first
        /// lattice plane clear of the surface: a small gap, never a penetration.
        /// </summary>
        private static PlacementProposal AgainstScenery(Vector3Int size, GridRotation rotation, Vector3 p, Vector3 normal)
        {
            int axis = DominantAxis(normal, out int sign);
            var min = new Vector3Int();
            var align = new Alignment[3];

            for (int i = 0; i < 3; i++)
            {
                if (i == axis)
                {
                    min[i] = sign > 0 ? Mathf.CeilToInt(p[i] - PlaneEpsilon) : Mathf.FloorToInt(p[i] + PlaneEpsilon) - size[i];
                    align[i] = Alignment.Contact;
                }
                else
                {
                    min[i] = Centred(p[i], size[i]);
                    align[i] = Alignment.Free;
                }
            }

            return new PlacementProposal(new CellBounds(min, size), rotation, Face(axis, sign), align[0], align[1], align[2]);
        }

        private PlacementProposal AgainstPiece(CellBounds target, Vector3Int size, GridRotation rotation, Vector3 p, Vector3 normal)
        {
            int axis = DominantAxis(normal, out int sign);
            if (axis != 1)
            {
                var edge = EdgeUnderAim(target, axis, p);
                if (edge != FaceEdge.None) return AcrossEdge(target, size, rotation, p, axis, sign, edge);
            }

            var min = new Vector3Int();
            var align = new Alignment[3];

            for (int i = 0; i < 3; i++)
            {
                if (i == axis)
                {
                    min[i] = sign > 0 ? target.Max[i] : target.Min[i] - size[i];
                    align[i] = Alignment.Contact;
                }
                else
                {
                    min[i] = AlignTangent(target.Min[i], target.Size[i], size[i], p[i], out align[i]);
                }
            }

            return new PlacementProposal(new CellBounds(min, size), rotation, Face(axis, sign), align[0], align[1], align[2]);
        }

        /// <summary>Which edge band of a side face the aim is in, if any. The nearest edge wins; top wins ties.</summary>
        private FaceEdge EdgeUnderAim(CellBounds target, int axis, Vector3 p)
        {
            int tangent = 2 - axis;
            var edge = FaceEdge.None;
            float nearest = float.MaxValue;

            void Consider(FaceEdge kind, float distance, int faceExtent)
            {
                float band = EdgeBand(faceExtent);
                if (band <= 0f || distance > band || distance >= nearest) return;
                nearest = distance;
                edge = kind;
            }

            Consider(FaceEdge.Top, target.Max.y - p.y, target.Size.y);
            Consider(FaceEdge.TangentLow, p[tangent] - target.Min[tangent], target.Size[tangent]);
            Consider(FaceEdge.TangentHigh, target.Max[tangent] - p[tangent], target.Size[tangent]);
            return edge;
        }

        /// <summary>Band depth in cells for a face <paramref name="faceExtent"/> cells across.</summary>
        private float EdgeBand(int faceExtent) =>
            Mathf.Min(_settings.EdgeBandFraction * faceExtent, _settings.EdgeBandMaxDepth / _grid.CellSize);

        /// <summary>
        /// Places across the aimed edge: on top of the target, or beside it continuing the row. The
        /// candidate stays flush with the face being aimed at, so stacks and rows keep a clean front.
        /// </summary>
        private PlacementProposal AcrossEdge(CellBounds target, Vector3Int size, GridRotation rotation, Vector3 p, int axis, int sign, FaceEdge edge)
        {
            int tangent = 2 - axis;
            var min = new Vector3Int();
            var align = new Alignment[3];

            min[axis] = sign > 0 ? target.Max[axis] - size[axis] : target.Min[axis];
            align[axis] = sign > 0 ? Alignment.Max : Alignment.Min;

            Vector3Int face;
            if (edge == FaceEdge.Top)
            {
                min.y = target.Max.y;
                align[1] = Alignment.Contact;
                min[tangent] = AlignTangent(target.Min[tangent], target.Size[tangent], size[tangent], p[tangent], out align[tangent]);
                face = Vector3Int.up;
            }
            else
            {
                bool high = edge == FaceEdge.TangentHigh;
                min[tangent] = high ? target.Max[tangent] : target.Min[tangent] - size[tangent];
                align[tangent] = Alignment.Contact;
                min.y = target.Min.y;
                align[1] = Alignment.Min;
                face = Face(tangent, high ? 1 : -1);
            }

            return new PlacementProposal(new CellBounds(min, size), rotation, face, align[0], align[1], align[2]);
        }

        /// <summary>Picks the candidate's low cell on one tangent axis (doc §9.3–9.5).</summary>
        private int AlignTangent(int targetMin, int targetSize, int size, float aim, out Alignment alignment)
        {
            float t = (aim - targetMin) / targetSize;
            var intent = t < _settings.LowerBand ? Alignment.Min : t >= _settings.UpperBand ? Alignment.Max : Alignment.Center;

            int bestMin = 0;
            float bestScore = float.MaxValue;
            var chosen = Alignment.Free;

            void Consider(Alignment kind, int candidateMin, float line)
            {
                float distance = Mathf.Abs(aim - line) * _grid.CellSize;
                if (distance > _settings.MagnetDistance) return;

                float score = distance + (kind == intent ? 0f : _settings.IntentMismatchPenalty);
                if (score >= bestScore) return;

                bestScore = score;
                bestMin = candidateMin;
                chosen = kind;
            }

            Consider(Alignment.Min, targetMin, targetMin);
            Consider(Alignment.Max, targetMin + targetSize - size, targetMin + targetSize);

            // Centres only line up on the lattice when the sizes differ by an even number of cells.
            int slack = targetSize - size;
            if (slack % 2 == 0) Consider(Alignment.Center, targetMin + slack / 2, targetMin + targetSize * 0.5f);

            alignment = chosen;
            return chosen == Alignment.Free ? Centred(aim, size) : bestMin;
        }

        /// <summary>The low cell of a run of <paramref name="size"/> cells centred as closely as the lattice allows on <paramref name="aim"/>.</summary>
        private static int Centred(float aim, int size) => RoundHalfUp(aim - size * 0.5f);

        private static int RoundHalfUp(float value) => Mathf.FloorToInt(value + 0.5f);

        private static int DominantAxis(Vector3 normal, out int sign)
        {
            float ax = Mathf.Abs(normal.x), ay = Mathf.Abs(normal.y), az = Mathf.Abs(normal.z);
            int axis = ay >= ax && ay >= az ? 1 : ax >= az ? 0 : 2;
            sign = normal[axis] >= 0f ? 1 : -1;
            return axis;
        }

        private static Vector3Int Face(int axis, int sign)
        {
            var face = Vector3Int.zero;
            face[axis] = sign;
            return face;
        }
    }
}
