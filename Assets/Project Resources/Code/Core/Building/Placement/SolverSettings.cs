using System;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>Tuning for <see cref="PlacementSolver"/>. Defaults are the values recorded in doc Appendix A.</summary>
    [Serializable]
    public class SolverSettings
    {
        [Tooltip("Aiming below this fraction of a target face means 'line up the low ends'.")]
        [Range(0f, 0.5f)] public float LowerBand = 0.33f;

        [Tooltip("Aiming above this fraction of a target face means 'line up the high ends'.")]
        [Range(0.5f, 1f)] public float UpperBand = 0.66f;

        [Tooltip("Metres. An edge or centre line closer than this to the aim snaps the piece to it.")]
        [Min(0f)] public float MagnetDistance = 0.76f;

        [Tooltip("Metres added to a candidate's distance when it disagrees with the low/centre/high band aimed at.")]
        [Min(0f)] public float IntentMismatchPenalty = 0.3f;

        [Tooltip("Scenery faces whose normal has at least this much up are treated as ground.")]
        [Range(0f, 1f)] public float GroundNormalMinY = 0.7f;

        [Tooltip("Depth of the edge bands on a piece's side faces, as a fraction of the face. Aiming in the top band places on top; in a side band, extends the row. 0 turns edge placement off.")]
        [Range(0f, 0.5f)] public float EdgeBandFraction = 0.25f;

        [Tooltip("Metres. The deepest an edge band gets, so tall walls keep most of their face for face placement.")]
        [Min(0f)] public float EdgeBandMaxDepth = 0.3f;
    }
}
