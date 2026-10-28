using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// One frame of build intent. Every field is a press edge, never a hold, so holding a trigger
    /// can't build a row of pieces or spend twice (doc §13.5).
    /// </summary>
    public struct BuildInputFrame
    {
        /// <summary>Place, select the piece under the pointer, or drop a moved piece.</summary>
        public bool Confirm;

        public bool Rotate;

        /// <summary>-1 / +1 to step through the palette while placing, 0 for no change.</summary>
        public int Cycle;

        /// <summary>A palette slot picked directly (number keys), or -1.</summary>
        public int Slot;

        /// <summary>Turns build mode on or off. While off, nothing else in the frame applies.</summary>
        public bool ToggleBuild;

        /// <summary>One step back: drop the held piece, close the modify menu, or cancel a move.</summary>
        public bool Back;

        public static BuildInputFrame Empty => new BuildInputFrame { Slot = -1 };
    }

    /// <summary>Adapts one device to <see cref="BuildInputFrame"/>. Swap the component to change devices.</summary>
    public abstract class BuildInputSource : MonoBehaviour
    {
        public abstract BuildInputFrame Poll();
    }
}
