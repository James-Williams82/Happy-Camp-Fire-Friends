using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>Supplies the build pointer ray. Swap the component to aim from a mouse, a controller or a hand.</summary>
    public abstract class AimSource : MonoBehaviour
    {
        public abstract bool TryGetRay(out Ray ray);
    }
}
