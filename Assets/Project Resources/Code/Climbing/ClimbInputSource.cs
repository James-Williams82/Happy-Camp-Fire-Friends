using UnityEngine;

namespace CampfireFriends.Climbing
{
    /// <summary>Whether each hand is closed to grip.</summary>
    public abstract class ClimbInputSource : MonoBehaviour
    {
        public abstract bool IsGripping(HandSide side);
    }
}
