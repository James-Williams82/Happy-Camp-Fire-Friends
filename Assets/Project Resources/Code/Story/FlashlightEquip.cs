using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>The player's own flashlight: hidden until they take one from the locker, then in their hand and on.</summary>
    public class FlashlightEquip : MonoBehaviour
    {
        [SerializeField] private GameObject _flashlight;

        private void Awake()
        {
            if (_flashlight != null) _flashlight.SetActive(false);
        }

        public void Equip()
        {
            if (_flashlight != null) _flashlight.SetActive(true);
        }
    }
}
