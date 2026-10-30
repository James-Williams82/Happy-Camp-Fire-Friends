using UnityEngine;

namespace CampfireFriends.Locomotion
{
    /// <summary>
    /// Quiet footfalls while walking: one step clip every stride, quicker and a touch louder when
    /// sprinting. A long running clip (a run of steps rather than one) loops while sprinting instead.
    /// </summary>
    public class Footsteps : MonoBehaviour
    {
        [SerializeField] private SlideMover _mover;
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _step;
        [SerializeField] private AudioClip _run;

        [SerializeField, Range(0f, 1f)] private float _walkVolume = 0.18f;
        [SerializeField, Range(0f, 1f)] private float _runVolume = 0.24f;
        [SerializeField, Min(0.2f)] private float _walkStride = 0.75f;
        [SerializeField, Min(0.2f)] private float _runStride = 1.1f;

        [Tooltip("A run clip longer than this is a run of steps, and loops while sprinting.")]
        [SerializeField, Min(0.1f)] private float _loopLongerThan = 1.2f;

        private float _travelled;
        private float _lastWalk;

        private bool RunLoops => _run != null && _run.length > _loopLongerThan;

        private void OnEnable()
        {
            if (_mover != null) _mover.Walked += OnWalked;
        }

        private void OnDisable()
        {
            if (_mover != null) _mover.Walked -= OnWalked;
        }

        private void Update()
        {
            // Stop the running loop once the sprint (or the walking) does.
            if (_source != null && _source.loop && _source.isPlaying && (Time.time - _lastWalk > 0.15f || !_mover.Sprinting))
                _source.Stop();
        }

        private void OnWalked(float distance, bool sprinting)
        {
            if (_source == null) return;
            _lastWalk = Time.time;

            if (sprinting && RunLoops)
            {
                if (_source.isPlaying && _source.clip == _run) return;
                _source.clip = _run;
                _source.loop = true;
                _source.volume = _runVolume;
                _source.pitch = 1f;
                _source.Play();
                return;
            }

            _travelled += distance;
            float stride = sprinting ? _runStride : _walkStride;
            if (_travelled < stride) return;
            _travelled = 0f;

            var clip = sprinting && _run != null ? _run : _step;
            if (clip == null) return;
            if (_source.loop) { _source.Stop(); _source.loop = false; }
            _source.pitch = Random.Range(0.92f, 1.08f);
            _source.PlayOneShot(clip, sprinting ? _runVolume : _walkVolume);
        }
    }
}
