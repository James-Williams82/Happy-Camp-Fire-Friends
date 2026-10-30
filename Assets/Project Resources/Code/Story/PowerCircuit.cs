using System.Collections;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// A power circuit: once its flag is up, its lights come on one after another (with a click and
    /// a hum where given), and stay on. Joining late, they're simply on.
    /// </summary>
    public class PowerCircuit : StoryListener
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag Flag => (Night01Flag)_flag;

        [SerializeField] private long _flag = (long)Night01Flag.SecurityCircuitOn;
        [SerializeField] private Light[] _lights = new Light[0];
        [SerializeField] private Renderer[] _glows = new Renderer[0];
        [SerializeField, Min(0f)] private float _stagger = 0.35f;
        [SerializeField] private AudioSource _switchOn;

        private bool _on;

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag != Flag || _on) return;
            _on = true;
            if (!live) { foreach (var l in _lights) if (l != null) l.enabled = true; Glow(); return; }
            if (_switchOn != null) _switchOn.Play();
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            foreach (var light in _lights)
            {
                if (light == null) continue;
                yield return new WaitForSeconds(_stagger);
                light.enabled = true;
            }
            Glow();
        }

        private void Glow()
        {
            foreach (var r in _glows) if (r != null) r.material.EnableKeyword("_EMISSION");
        }
    }
}
