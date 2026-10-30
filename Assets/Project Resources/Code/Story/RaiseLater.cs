using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>Reports a fact a moment after being triggered: the bucket falls while someone is still rummaging in the cabinet.</summary>
    public class RaiseLater : MonoBehaviour
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag Flag => (Night01Flag)_flag;

        [SerializeField] private long _flag = (long)Night01Flag.BucketFell;
        [SerializeField, Min(0f)] private float _delay = 2.5f;

        public void Trigger() => StartCoroutine(Later());

        private IEnumerator Later()
        {
            yield return new WaitForSeconds(_delay);
            var story = Stories.Current;
            if (story != null && !story.State.Has(Flag)) story.Raise(Flag);
        }
    }
}
