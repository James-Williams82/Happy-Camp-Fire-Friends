using System;

namespace CampfireFriends.Building
{
    public enum BuildMode
    {
        /// <summary>Not building. The tool ignores everything but the build toggle.</summary>
        None,

        /// <summary>Building with nothing in hand: point at a placed piece and confirm to select it.</summary>
        Browse,

        /// <summary>Placing the palette piece picked from the menu.</summary>
        Place,

        /// <summary>A placed piece is selected; its modify menu offers rotate, reposition and delete.</summary>
        Modify,

        /// <summary>Repositioning the selected piece: its ghost follows the aim until confirmed.</summary>
        Move,
    }

    /// <summary>
    /// The local build tool's model: which mode it is in, which palette piece or placed piece it is
    /// working with, and how the ghost is turned. Input adapters and menus translate presses into
    /// these commands; views read the result.
    ///
    /// Entering build mode selects nothing: a piece to place has to be picked from the menu.
    /// </summary>
    public sealed class BuildToolState
    {
        public const int NoSelection = -1;

        private GridRotation _placeRotation;
        private GridRotation _moveRotation;

        public BuildMode Mode { get; private set; }

        /// <summary>The palette piece being placed, or <see cref="NoSelection"/>.</summary>
        public int SelectedIndex { get; private set; } = NoSelection;

        /// <summary>The placed piece being modified or moved, or <see cref="BuildWorld.NoPiece"/>.</summary>
        public int TargetId { get; private set; } = BuildWorld.NoPiece;

        public int PieceCount { get; }

        /// <summary>The ghost's rotation: the palette piece's while placing, the moved piece's while moving.</summary>
        public GridRotation Rotation => Mode == BuildMode.Move ? _moveRotation : _placeRotation;

        public bool IsBuilding => Mode != BuildMode.None;

        public event Action Changed;

        public BuildToolState(int pieceCount)
        {
            PieceCount = Math.Max(0, pieceCount);
        }

        /// <summary>Enters build mode with nothing selected, or leaves it from any mode.</summary>
        public void ToggleBuild() => Enter(IsBuilding ? BuildMode.None : BuildMode.Browse, NoSelection, BuildWorld.NoPiece);

        /// <summary>Picks a palette piece and starts placing it. Ignored outside build mode or out of range.</summary>
        public void Select(int index)
        {
            if (!IsBuilding || index < 0 || index >= PieceCount) return;
            Enter(BuildMode.Place, index, BuildWorld.NoPiece);
        }

        /// <summary>Steps to the next or previous palette piece, wrapping. Only while placing.</summary>
        public void Cycle(int direction)
        {
            if (Mode != BuildMode.Place || PieceCount == 0 || direction == 0) return;
            int next = ((SelectedIndex + Math.Sign(direction)) % PieceCount + PieceCount) % PieceCount;
            Enter(BuildMode.Place, next, BuildWorld.NoPiece);
        }

        /// <summary>Turns the ghost a quarter turn about Y. Only while placing or moving.</summary>
        public void Rotate(int quarterTurns = 1)
        {
            if (Mode == BuildMode.Place) _placeRotation = _placeRotation.Rotated(quarterTurns);
            else if (Mode == BuildMode.Move) _moveRotation = _moveRotation.Rotated(quarterTurns);
            else return;

            Changed?.Invoke();
        }

        /// <summary>Selects a placed piece and opens its modify menu.</summary>
        public void Inspect(int placedId)
        {
            if (!IsBuilding || placedId == BuildWorld.NoPiece) return;
            Enter(BuildMode.Modify, NoSelection, placedId);
        }

        /// <summary>Starts repositioning the selected piece, its ghost turned as the piece is now.</summary>
        public void BeginMove(GridRotation currentRotation)
        {
            if (Mode != BuildMode.Modify) return;
            _moveRotation = currentRotation;
            Enter(BuildMode.Move, NoSelection, TargetId);
        }

        /// <summary>One step back: moving → modify menu; placing or modifying → nothing selected.</summary>
        public void Back()
        {
            if (Mode == BuildMode.Move) Enter(BuildMode.Modify, NoSelection, TargetId);
            else if (Mode == BuildMode.Place || Mode == BuildMode.Modify) Deselect();
        }

        /// <summary>Done with the selected piece (deleted, moved, or dismissed): back to nothing selected.</summary>
        public void Deselect()
        {
            if (IsBuilding) Enter(BuildMode.Browse, NoSelection, BuildWorld.NoPiece);
        }

        private void Enter(BuildMode mode, int index, int target)
        {
            if (mode == Mode && index == SelectedIndex && target == TargetId) return;

            Mode = mode;
            SelectedIndex = index;
            TargetId = target;
            Changed?.Invoke();
        }
    }
}
