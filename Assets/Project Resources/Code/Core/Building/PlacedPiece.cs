namespace CampfireFriends.Building
{
    /// <summary>
    /// A committed piece in the shared world. Ids are durable and positive so incidents, damage,
    /// saves and the network can all refer to the same piece.
    /// </summary>
    public sealed class PlacedPiece
    {
        public int Id { get; }
        public string PieceId { get; }
        public CellBounds Bounds { get; }
        public GridRotation Rotation { get; }
        public int PlacedBy { get; }

        /// <summary>Changed only through the damage/repair service.</summary>
        public int Health { get; set; }

        /// <summary>Whether a toggle piece (a light) is switched on. Changed only through <see cref="BuildWorld.SetOn"/>.</summary>
        public bool On { get; internal set; }

        public PlacedPiece(int id, string pieceId, CellBounds bounds, GridRotation rotation, int placedBy, int health, bool on = false)
        {
            On = on;
            Id = id;
            PieceId = pieceId;
            Bounds = bounds;
            Rotation = rotation;
            PlacedBy = placedBy;
            Health = health;
        }

        public override string ToString() => $"{PieceId} #{Id} {Bounds} {Rotation}";
    }
}
