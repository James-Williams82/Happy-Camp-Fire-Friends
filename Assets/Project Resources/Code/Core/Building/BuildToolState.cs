using System;

namespace CampfireFriends.Building
{
    public enum BuildMode
    {
        None,
        Place,
        Delete,
    }

    /// <summary>
    /// The local build tool's model: which mode it is in, which piece is selected and how it is turned.
    /// Input adapters translate presses into these commands; views read the result.
    /// </summary>
    public sealed class BuildToolState
    {
        public BuildMode Mode { get; private set; }
        public int SelectedIndex { get; private set; }
        public GridRotation Rotation { get; private set; }
        public int PieceCount { get; }

        public event Action Changed;

        public BuildToolState(int pieceCount)
        {
            PieceCount = Math.Max(0, pieceCount);
        }

        /// <summary>Selects a palette slot and starts placing it. Out-of-range slots are ignored.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= PieceCount) return;
            Set(BuildMode.Place, index, Rotation);
        }

        /// <summary>Steps through the palette, wrapping, and starts placing.</summary>
        public void Cycle(int direction)
        {
            if (PieceCount == 0 || direction == 0) return;
            int next = Mode == BuildMode.Place ? ((SelectedIndex + Math.Sign(direction)) % PieceCount + PieceCount) % PieceCount : SelectedIndex;
            Set(BuildMode.Place, next, Rotation);
        }

        public void Rotate(int quarterTurns = 1) => Set(Mode, SelectedIndex, Rotation.Rotated(quarterTurns));

        /// <summary>Switches between placing and deleting; from idle it starts placing.</summary>
        public void ToggleDelete() => Set(Mode == BuildMode.Place ? BuildMode.Delete : BuildMode.Place, SelectedIndex, Rotation);

        /// <summary>Steps idle → place → delete → idle, for controllers with few buttons.</summary>
        public void CycleMode() => Set((BuildMode)(((int)Mode + 1) % 3), SelectedIndex, Rotation);

        public void Cancel() => Set(BuildMode.None, SelectedIndex, Rotation);

        private void Set(BuildMode mode, int index, GridRotation rotation)
        {
            if (mode == BuildMode.Place && PieceCount == 0) mode = BuildMode.None;
            if (mode == Mode && index == SelectedIndex && rotation == Rotation) return;

            Mode = mode;
            SelectedIndex = index;
            Rotation = rotation;
            Changed?.Invoke();
        }
    }
}
