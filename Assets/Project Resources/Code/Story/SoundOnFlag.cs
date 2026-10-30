using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>A sound (or a short run of them) at a spot in the world when a flag goes up live: knocks, a clang, a hit.</summary>
    public class SoundOnFlag : StoryListener
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag Flag => (Night01Flag)_flag;

        [SerializeField] private long _flag;
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];

        [Tooltip("How many times (three knocks), and the gap between them.")]
        [SerializeField, Min(1)] private int _repeats = 1;
        [SerializeField, Min(0f)] private float _gap = 0.55f;
        [SerializeField, Min(0f)] private float _delay;

        [Tooltip("Use generated wooden knocks (no recording needed).")]
        [SerializeField] private bool _knocks;

        private void Awake()
        {
            if (_knocks) _clips = new[] { StorySounds.Knock(5), StorySounds.Knock(6), StorySounds.Knock(9) };
        }

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag != Flag || !live || _source == null) return;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            if (_delay > 0f) yield return new WaitForSeconds(_delay);
            for (int i = 0; i < _repeats; i++)
            {
                var clip = _clips.Length > 0 ? _clips[Random.Range(0, _clips.Length)] : _source.clip;
                if (clip != null) _source.PlayOneShot(clip);
                if (i < _repeats - 1) yield return new WaitForSeconds(_gap);
            }
        }
    }
}
