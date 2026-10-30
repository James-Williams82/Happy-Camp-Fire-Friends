using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>Shows some objects and hides others once a flag is up (a fuse leaves its cabinet and appears in the panel).</summary>
    public class ShowOnFlag : StoryListener
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag Flag => (Night01Flag)_flag;

        [SerializeField] private long _flag;
        [SerializeField] private GameObject[] _show = new GameObject[0];
        [SerializeField] private GameObject[] _hide = new GameObject[0];

        protected override void OnBound() => Apply(Story.State.Has(Flag));

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag == Flag) Apply(true);
        }

        private void Apply(bool raised)
        {
            foreach (var go in _show) if (go != null) go.SetActive(raised);
            foreach (var go in _hide) if (go != null) go.SetActive(!raised);
        }
    }
}
