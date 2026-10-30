using UnityEngine;

namespace CampfireFriends.Arrival
{
    /// <summary>Makes a light breathe like a fire: uneven brightness and a little drift.</summary>
    [RequireComponent(typeof(Light))]
    public class FireFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _depth = 0.3f;
        [SerializeField, Min(0.1f)] private float _speed = 6f;

        private Light _light;
        private float _base;
        private Vector3 _home;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _home = transform.localPosition;
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            float t = Time.time * _speed;
            float n = Mathf.PerlinNoise(_seed, t) * 0.7f + Mathf.PerlinNoise(_seed + 7f, t * 2.3f) * 0.3f;
            _light.intensity = _base * (1f - _depth + _depth * 2f * n);
            transform.localPosition = _home + new Vector3(n - 0.5f, 0f, Mathf.PerlinNoise(_seed + 3f, t) - 0.5f) * 0.06f;
        }
    }
}
