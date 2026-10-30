using System.Collections.Generic;
using CampfireFriends.Supplies;
using UnityEngine;

namespace CampfireFriends.Building
{
    /// <summary>How big a piece feels: picks its placement sound and other size-scaled feedback.</summary>
    public enum PieceSize
    {
        Small,
        Medium,
        Large,
    }

    /// <summary>What happens when a player points at a placed piece and uses it (trigger, build mode off).</summary>
    public enum PieceUse
    {
        None,

        /// <summary>Switches the piece on and off, e.g. a light. Everyone sees the same state.</summary>
        Toggle,
    }

    /// <summary>
    /// One buildable piece. The footprint is the logical volume in grid cells and is the single
    /// source of geometric truth: placement, occupancy, support, the target collider and the
    /// preview are all derived from it, never from the decorative mesh (doc §5.4, §6.2).
    /// </summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Building/Piece Definition", fileName = "Piece_")]
    public class PieceDefinition : ScriptableObject
    {
        [Tooltip("Durable id used in saves and network messages. Never rename once shipped.")]
        public string Id;

        public string DisplayName;

        [Tooltip("Optional extra line for the build menu tooltip: what's special about this piece.")]
        public string Hint = "";

        [Tooltip("Logical size in 0.5 m cells at 0° (x = width, y = height, z = depth).")]
        public Vector3Int Footprint = Vector3Int.one;

        public SupportRule Support = SupportRule.Stack;

        public List<ResourceAmount> Cost = new List<ResourceAmount>();

        [Tooltip("Whether players can take this piece down again.")]
        public bool Removable = true;

        [Tooltip("Return the full cost to the shared stock when the piece is removed on purpose.")]
        public bool RefundOnRemove = true;

        [Min(1)] public int MaxHealth = 100;

        [Tooltip("Picks the size-specific placement sound.")]
        public PieceSize Size = PieceSize.Medium;

        [Tooltip("What using the placed piece does. None means it can't be used.")]
        public PieceUse Use = PieceUse.None;

        [Tooltip("Metres from the player's hand to the piece within which it can be used.")]
        [Min(0.1f)] public float UseRange = 2.5f;

        [Tooltip("Pieces with the same group join up visually where they meet end-on, e.g. \"fence\" for fence panels and corners. Empty joins nothing.")]
        public string ConnectGroup = "";

        [Tooltip("A variant of the base piece prefab, with its pivot at the bottom-centre of the footprint.")]
        public GameObject Prefab;

        public CellBounds BoundsAt(Vector3Int min, GridRotation rotation) => CellBounds.ForPiece(min, Footprint, rotation);

        private void OnValidate()
        {
            Footprint = Vector3Int.Max(Footprint, Vector3Int.one);
        }
    }
}
