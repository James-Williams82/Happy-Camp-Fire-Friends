using UnityEngine;

namespace CampfireFriends.Arrival
{
    /// <summary>
    /// Bixby, in person, on the ferry: talks the crew across the lake and sees them off at the
    /// camp dock, then stays aboard. His crossing lines are timed by the ferry's shared time under
    /// way, so every player hears the same line at the same moment, and someone joining mid-way
    /// picks up at the next line. A crew that never rode across (a later session) hears nothing.
    /// </summary>
    public class FerryHost : MonoBehaviour
    {
        [SerializeField] private Ferry _ferry;
        [SerializeField] private AudioSource _voice;

        [Tooltip("Spoken while crossing, counted from cast-off. The ferry's crossing time is set to fit them.")]
        [SerializeField] private SpokenLine[] _crossing = new SpokenLine[0];

        [Tooltip("Spoken once the ferry is tied up at the camp dock.")]
        [SerializeField] private SpokenLine[] _docked = new SpokenLine[0];

        [Tooltip("Seconds after cast-off before he starts (the bell and the horn go first).")]
        [SerializeField, Min(0f)] private float _startDelay = 4f;

        [Tooltip("Seconds after tying up (and finishing his last crossing line) before the send-off.")]
        [SerializeField, Min(0f)] private float _dockedDelay = 1.2f;

        [Tooltip("Turns to face whoever's listening. Optional.")]
        [SerializeField] private Transform _body;

        // A line whose start was missed by more than this is skipped rather than started late.
        private const float LateStart = 1f;

        private bool _rode;
        private int _next;
        private float _dockedAt = -1f;
        private Vector3 _rest;

        public float CrossingLength => _startDelay + Total(_crossing);

        private void Awake()
        {
            if (_body != null) _rest = _body.localPosition;
        }

        private void Update()
        {
            if (_ferry == null || _voice == null) return;

            switch (_ferry.Ride.State)
            {
                case FerryState.Crossing:
                    if (!_rode) { _rode = true; _next = 0; }
                    Speak(_crossing, _ferry.Ride.UnderWay - _startDelay);
                    break;

                case FerryState.Arrived when _rode:
                    if (_dockedAt < 0f)
                    {
                        if (_voice.isPlaying) break;
                        _dockedAt = Time.time;
                        _next = 0;
                    }
                    Speak(_docked, Time.time - _dockedAt - _dockedDelay);
                    break;
            }

            Animate();
        }

        /// <summary>Starts whichever line is due at <paramref name="t"/> seconds into the passage.</summary>
        private void Speak(SpokenLine[] lines, float t)
        {
            float start = 0f;
            for (int i = 0; i < _next && i < lines.Length; i++) start += lines[i].Seconds;

            while (_next < lines.Length && start <= t)
            {
                var line = lines[_next];
                if (t - start <= LateStart && line.Clip != null)
                {
                    _voice.Stop();
                    _voice.clip = line.Clip;
                    _voice.Play();
                }
                start += line.Seconds;
                _next++;
            }
        }

        /// <summary>Faces the listener and bobs a little while talking, so it's clear who's speaking.</summary>
        private void Animate()
        {
            if (_body == null) return;

            var cam = Camera.main;
            if (cam != null)
            {
                var toward = Vector3.ProjectOnPlane(cam.transform.position - _body.position, Vector3.up);
                if (toward.sqrMagnitude > 0.01f)
                    _body.rotation = Quaternion.Slerp(_body.rotation, Quaternion.LookRotation(toward), 1f - Mathf.Exp(-Time.deltaTime * 3f));
            }

            float bob = _voice.isPlaying ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.025f : 0f;
            _body.localPosition = _rest + Vector3.up * bob;
        }

        private static float Total(SpokenLine[] lines)
        {
            float total = 0f;
            foreach (var line in lines) total += line.Seconds;
            return total;
        }
    }
}
