using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>The end card in the office: NIGHT 1 COMPLETE.</summary>
    public class EndCard : StoryListener
    {
        [SerializeField] private GameObject _card;
        [SerializeField, Min(0f)] private float _delay = 12f;

        protected override void OnBound() { if (_card != null) _card.SetActive(Story.State.Has(Night01Flag.Completed)); }

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag != Night01Flag.Completed || _card == null) return;
            if (!live) { _card.SetActive(true); return; }
            StartCoroutine(ShowLater());
        }

        private IEnumerator ShowLater()
        {
            yield return new WaitForSeconds(_delay);
            _card.SetActive(true);
        }
    }
}
