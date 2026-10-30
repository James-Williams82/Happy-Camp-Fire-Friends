using System.Collections;
using UnityEngine;

namespace CampfireFriends.Arrival
{
    /// <summary>
    /// The ferry's voice: the engine idles at the landing and opens up with the boat's speed, the
    /// bell rings when it's rung, and the horn sounds as it casts off and again as it nears the
    /// camp dock. Driven by the ferry's shared state, so every player hears the same moments.
    /// </summary>
    public class FerrySounds : MonoBehaviour
    {
        [SerializeField] private Ferry _ferry;
        [SerializeField] private AudioSource _engine;
        [SerializeField] private AudioSource _calls;
        [SerializeField] private AudioClip _bell;
        [SerializeField] private AudioClip _horn;

        [Header("Engine")]
        [SerializeField, Range(0f, 1f)] private float _idleVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _fullVolume = 0.8f;
        [SerializeField] private Vector2 _pitch = new Vector2(0.85f, 1.1f);
        [Tooltip("Speed (m/s) at which the engine is at full volume and pitch.")]
        [SerializeField, Min(0.1f)] private float _fullSpeed = 2.3f;

        [Tooltip("How far across (0–1) the approach horn sounds.")]
        [SerializeField, Range(0.5f, 0.98f)] private float _approachAt = 0.85f;

        private Vector3 _last;
        private float _speed;
        private bool _approachHorn;

        private void OnEnable()
        {
            if (_ferry == null) return;
            _last = _ferry.transform.position;
            _ferry.StateChanged += OnStateChanged;
            if (_engine != null && !_engine.isPlaying) _engine.Play();
        }

        private void OnDisable()
        {
            if (_ferry != null) _ferry.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (_ferry == null || _engine == null) return;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = Vector3.Distance(_ferry.transform.position, _last) / dt;
            _last = _ferry.transform.position;
            _speed = Mathf.Lerp(_speed, speed, 1f - Mathf.Exp(-dt * 3f));

            float k = Mathf.Clamp01(_speed / _fullSpeed);
            _engine.volume = Mathf.Lerp(_idleVolume, _fullVolume, k);
            _engine.pitch = Mathf.Lerp(_pitch.x, _pitch.y, k);

            if (!_approachHorn && _ferry.Ride.State == FerryState.Crossing && _ferry.Ride.Progress >= _approachAt)
            {
                _approachHorn = true;
                Call(_horn);
            }
        }

        private void OnStateChanged(FerryState state)
        {
            if (state != FerryState.Crossing) return;
            StartCoroutine(CastOff());
        }

        private IEnumerator CastOff()
        {
            Call(_bell);
            yield return new WaitForSeconds(1.6f);
            Call(_horn);
        }

        private void Call(AudioClip clip)
        {
            if (_calls != null && clip != null) _calls.PlayOneShot(clip);
        }
    }
}
