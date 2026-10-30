using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>A light piece: switched on, its light shines and its bulb glows; off, both go dark.</summary>
    public class LightStateView : PieceStateView
    {
        [Tooltip("Saved disabled, so previews and pieces that are off don't light the scene.")]
        [SerializeField] private Light _light;

        [SerializeField] private Renderer _bulb;
        [SerializeField] private Material _bulbOn;
        [SerializeField] private Material _bulbOff;

        public override void Show(bool on)
        {
            if (_light != null) _light.enabled = on;
            if (_bulb != null)
            {
                var material = on ? _bulbOn : _bulbOff;
                if (material != null) _bulb.sharedMaterial = material;
            }
        }
    }
}
