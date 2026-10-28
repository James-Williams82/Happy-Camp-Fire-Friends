namespace CampfireFriends.Building
{
    /// <summary>What has to be under or beside a piece for it to stay up (doc §10.7, §12.3).</summary>
    public enum SupportRule
    {
        /// <summary>Every bottom cell rests on ground or on another piece. Blocks, walls, floors.</summary>
        Stack = 0,

        /// <summary>Every bottom cell rests on the ground itself. Foundations, fence panels.</summary>
        Ground = 1,

        /// <summary>
        /// Rests on a piece, or shares an edge with a roof panel that is itself held up by a piece.
        /// Ground never holds up a roof, and touching only at a corner never counts.
        /// </summary>
        Roof = 2,

        /// <summary>Touches a piece on any side, or rests on ground or a piece. Beams, boards, fixtures.</summary>
        SideAttach = 3,

        /// <summary>Always supported. For debugging and authored set dressing.</summary>
        None = 4,
    }
}
