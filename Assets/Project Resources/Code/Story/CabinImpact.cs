using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// The impact on Cabin 6 (the finale): one heavy hit from inside. The boards over the opening
    /// jolt in place and dust puffs out; they stay where the players put them. If nothing was
    /// built there, the hit lands on the cabin and the dust still comes out.
    /// </summary>
    public class CabinImpact : StoryListener
    {
        [Tooltip("Where the opening is; build pieces within this box take the hit.")]
        [SerializeField] private BoxCollider _opening;
        [SerializeField] private AudioSource _hit;
        [SerializeField] private ParticleSystem _dust;
        [SerializeField, Min(0f)] private float _jolt = 0.04f;

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag != Night01Flag.CabinImpactPlayed || !live) return;
            if (_hit != null) _hit.Play();
            if (_dust != null) _dust.Play();
            if (_opening == null) return;

            var bounds = _opening.bounds;
            bounds.Expand(0.6f);
            foreach (var piece in FindObjectsByType<CampfireFriends.Construction.PieceView>(FindObjectsSortMode.None))
                if (piece.PlacedId != CampfireFriends.Building.BuildWorld.NoPiece && bounds.Contains(piece.transform.position + Vector3.up * 0.5f))
                    StartCoroutine(Jolt(piece.transform));
        }

        private IEnumerator Jolt(Transform piece)
        {
            var home = piece.position;
            var outward = _opening.transform.right;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                float k = Mathf.Exp(-t * 9f) * Mathf.Sin(t * 70f);
                piece.position = home + outward * (_jolt * k) + Random.insideUnitSphere * (_jolt * 0.2f * Mathf.Exp(-t * 9f));
                yield return null;
            }
            piece.position = home;
        }
    }
}
