using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>The falling bucket (SE-01): tips off its shelf, drops, clangs, and stays on the floor.</summary>
    public class BucketFall : StoryListener
    {
        [SerializeField] private Transform _bucket;
        [SerializeField] private Vector3 _floorOffset = new Vector3(-0.6f, -1.8f, 0.3f);
        [SerializeField] private AudioSource _clang;

        private bool _fallen;

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag != Night01Flag.BucketFell || _fallen || _bucket == null) return;
            _fallen = true;

            var start = _bucket.position;
            var end = start + _floorOffset;
            var endRotation = Quaternion.Euler(85f, Random.Range(0f, 360f), 0f);
            if (!live) { _bucket.SetPositionAndRotation(end, endRotation); return; }
            StartCoroutine(Fall(start, end, endRotation));
        }

        private IEnumerator Fall(Vector3 start, Vector3 end, Quaternion endRotation)
        {
            var startRotation = _bucket.rotation;
            float duration = Mathf.Sqrt(2f * Mathf.Abs(end.y - start.y) / 9.81f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                _bucket.SetPositionAndRotation(
                    new Vector3(Mathf.Lerp(start.x, end.x, k), Mathf.Lerp(start.y, end.y, k * k), Mathf.Lerp(start.z, end.z, k)),
                    Quaternion.Slerp(startRotation, endRotation, k));
                yield return null;
            }
            _bucket.SetPositionAndRotation(end, endRotation);
            if (_clang != null) { _clang.transform.position = end; _clang.Play(); }
        }
    }
}
