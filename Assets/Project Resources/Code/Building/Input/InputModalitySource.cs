using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Says whether the player is using tracked hands or controllers right now, so views can fit
    /// themselves to it (hands want things within arm's reach). No source means controllers.
    /// </summary>
    public abstract class InputModalitySource : MonoBehaviour
    {
        public abstract bool UsingHands { get; }
    }
}
