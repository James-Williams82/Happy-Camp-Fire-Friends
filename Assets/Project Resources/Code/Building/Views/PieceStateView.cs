using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Shows a placed piece's in-place state, such as a light switched on. Put one on a piece prefab
    /// variant; its <see cref="PieceView"/> passes the state on. Previews never call it, so a ghost
    /// stays in the prefab's saved (off) look.
    /// </summary>
    public abstract class PieceStateView : MonoBehaviour
    {
        public abstract void Show(bool on);
    }
}
