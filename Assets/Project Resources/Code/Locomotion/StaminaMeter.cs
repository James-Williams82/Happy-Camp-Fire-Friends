using UnityEngine;

namespace CampfireFriends.Locomotion
{
    /// <summary>
    /// The sprint meter on the left wrist: a bar that empties while sprinting and refills at rest,
    /// red when winded. Shown only while it isn't full.
    /// </summary>
    public class StaminaMeter : MonoBehaviour
    {
        [SerializeField] private SlideMover _mover;
        [SerializeField] private GameObject _root;
        [Tooltip("Scaled along x from its left end.")]
        [SerializeField] private Transform _fill;
        [SerializeField] private Renderer _fillRenderer;
        [SerializeField] private Color _ready = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] private Color _winded = new Color(0.85f, 0.3f, 0.25f);

        private Vector3 _fullScale;
        private Vector3 _fullPosition;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            if (_fill == null) return;
            _fullScale = _fill.localScale;
            _fullPosition = _fill.localPosition;
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            var stamina = _mover != null ? _mover.Stamina : null;
            if (stamina == null || _fill == null) return;

            bool show = stamina.Fraction < 0.999f;
            if (_root != null && _root.activeSelf != show) _root.SetActive(show);
            if (!show) return;

            float f = Mathf.Max(0.001f, stamina.Fraction);
            _fill.localScale = new Vector3(_fullScale.x * f, _fullScale.y, _fullScale.z);
            _fill.localPosition = _fullPosition - Vector3.right * (_fullScale.x * (1f - f) * 0.5f);

            if (_fillRenderer != null)
            {
                var colour = stamina.Winded ? _winded : _ready;
                _fillRenderer.GetPropertyBlock(_block);
                _block.SetColor("_BaseColor", colour);
                _block.SetColor("_Color", colour);
                _fillRenderer.SetPropertyBlock(_block);
            }
        }
    }
}
