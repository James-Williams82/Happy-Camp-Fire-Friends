using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// SE-02: a player alone on the path hears two steps keeping pace beside them, then one on a
    /// wooden walkway. No sting, no repeat, and only on that player's machine: the others may
    /// never hear it, so nobody's progress depends on it.
    /// </summary>
    public class FootstepScare : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private AudioSource _voice;
        [SerializeField, Min(0.5f)] private float _sideDistance = 3.5f;

        private AudioClip _step, _wood;
        private bool _played;

        private void Awake()
        {
            _step = StorySounds.Step(11);
            _wood = StorySounds.WoodStep(12);
        }

        private void OnDestroy()
        {
            if (_step != null) Destroy(_step);
            if (_wood != null) Destroy(_wood);
        }

        public void Play()
        {
            if (_played || _head == null || _voice == null) return;
            _played = true;
            StartCoroutine(Steps());
        }

        private IEnumerator Steps()
        {
            var side = Vector3.ProjectOnPlane(_head.right, Vector3.up).normalized * (Random.value < 0.5f ? -1f : 1f);
            for (int i = 0; i < 2; i++)
            {
                var forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                _voice.transform.position = _head.position + side * _sideDistance + forward * (i * 0.6f) + Vector3.down * 1.5f;
                _voice.PlayOneShot(_step, 0.7f);
                yield return new WaitForSeconds(0.55f);
            }
            yield return new WaitForSeconds(0.8f);
            _voice.transform.position = _head.position + side * (_sideDistance + 2f) + Vector3.down * 1.4f;
            _voice.PlayOneShot(_wood, 0.8f);
        }
    }
}
