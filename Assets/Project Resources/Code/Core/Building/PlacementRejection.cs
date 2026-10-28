namespace CampfireFriends.Building
{
    /// <summary>Why a placement or removal was refused. Each reason is distinct so a red preview can be explained.</summary>
    public enum PlacementRejection
    {
        None = 0,
        UnknownPiece,
        NoTarget,
        Occupied,
        Obstructed,
        NoSupport,
        InsufficientMaterials,
        NotRemovable,
        UnknownPlacedPiece,
        HasDependents,
    }
}
