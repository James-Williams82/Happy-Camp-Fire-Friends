using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>Turns a hinge (a gate leaf, a door) to its open angle once a flag is up.</summary>
    public class SwingOnFlag : StoryListener
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag Flag => (Night01Flag)_flag;

        [SerializeField] private long _flag;
        [SerializeField] private Transform _hinge;
        [SerializeField] private Vector3 _openEuler = new Vector3(0f, 100f, 0f);
        [SerializeField, Min(0.05f)] private float _seconds = 1.6f;
        [SerializeField] private AudioSource _sound;

        private Quaternion _closed;
        private bool _open;

        private void Awake()
        {
            if (_hinge != null) _closed = _hinge.localRotation;
        }

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag != Flag || _open || _hinge == null) return;
            _open = true;
            var open = _closed * Quaternion.Euler(_openEuler);
            if (!live) { _hinge.localRotation = open; return; }
            if (_sound != null) _sound.Play();
            StartCoroutine(Swing(open));
        }

        private IEnumerator Swing(Quaternion open)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / _seconds)
            {
                _hinge.localRotation = Quaternion.Slerp(_closed, open, t * t * (3f - 2f * t));
                yield return null;
            }
            _hinge.localRotation = open;
        }
    }
}
