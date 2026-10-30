using UnityEngine;

namespace CampfireFriends.Climbing
{
    /// <summary>
    /// Marks this object's colliders as something to climb: ladder rungs, a climbing wall, a
    /// lifeguard tower. Gripping inside or near any of its colliders takes hold.
    /// </summary>
    public class Climbable : MonoBehaviour
    {
        [Tooltip("Letting go above the top pulls the player up and over onto what's behind it, if there is somewhere to stand.")]
        [SerializeField] private bool _mantleAtTop = true;

        public bool MantleAtTop => _mantleAtTop;

        /// <summary>The highest point of this climbable's colliders.</summary>
        public float Top
        {
            get
            {
                float top = transform.position.y;
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c.enabled) top = Mathf.Max(top, c.bounds.max.y);
                return top;
            }
        }
    }
}
