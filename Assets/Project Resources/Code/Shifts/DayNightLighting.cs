using UnityEngine;
using UnityEngine.Rendering;

namespace CampfireFriends.Shifts
{
    /// <summary>
    /// Shows the shift clock's time of day: turns the sun and moon, colours them, and sets the
    /// ambient light, fog and sky to match. Purely a view; it follows whichever clock is in charge.
    /// The sun drives the sky (so dusk reddens and night falls naturally); the moon is a dim second
    /// light that keeps the camp readable after dark.
    /// </summary>
    public class DayNightLighting : MonoBehaviour
    {
        [SerializeField] private DayNightLook _look;

        [Tooltip("Directional light that is the sun. Also the sky's sun.")]
        [SerializeField] private Light _sun;

        [Tooltip("Directional light for moonlight. Optional.")]
        [SerializeField] private Light _moon;

        [Tooltip("Turns the whole sky about the vertical, so north is where the camp needs it.")]
        [SerializeField, Range(-180f, 180f)] private float _northYaw;

        [Tooltip("Shown while no clock is running, e.g. in the editor.")]
        [SerializeField, Range(0f, 24f)] private float _previewHour = 15f;

        private Material _sky;
        private bool _hasExposure;

        private void Start()
        {
            // Work on a copy, so tinting the sky never edits the scene's skybox asset.
            if (RenderSettings.skybox != null)
            {
                _sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _sky;
                _hasExposure = _sky.HasProperty("_Exposure");
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            if (_sun != null) RenderSettings.sun = _sun;
        }

        private void OnDestroy()
        {
            if (_sky != null) Destroy(_sky);
        }

        private void LateUpdate()
        {
            var source = ShiftClocks.Current;
            float hour = source?.Clock != null ? source.Clock.Hours : _previewHour;
            var path = source?.Settings != null ? source.Settings.Sun : SunPath.Default;
            Apply(hour, path);
        }

        private void Apply(float hour, SunPath path)
        {
            if (_look == null) return;
            float key = DayNightLook.Key(hour);

            path.Sun(hour, out float sunElevation, out float sunAzimuth);
            Aim(_sun, sunElevation, sunAzimuth, _look.SunColor.Evaluate(key), _look.SunIntensity.Evaluate(key));

            path.Moon(hour, out float moonElevation, out float moonAzimuth);
            Aim(_moon, moonElevation, moonAzimuth, _look.MoonColor, _look.MoonIntensity.Evaluate(key));

            RenderSettings.ambientSkyColor = _look.AmbientSky.Evaluate(key);
            RenderSettings.ambientEquatorColor = _look.AmbientEquator.Evaluate(key);
            RenderSettings.ambientGroundColor = _look.AmbientGround.Evaluate(key);
            RenderSettings.fogColor = _look.FogColor.Evaluate(key);
            RenderSettings.fogDensity = Mathf.Max(0f, _look.FogDensity.Evaluate(key));
            if (_hasExposure) _sky.SetFloat("_Exposure", Mathf.Max(0f, _look.SkyExposure.Evaluate(key)));
        }

        /// <summary>Points a light from the given spot in the sky, and switches it off when it would be lighting the camp from below.</summary>
        private void Aim(Light light, float elevation, float azimuth, Color color, float intensity)
        {
            if (light == null) return;

            // A light shines along its forward: from its spot in the sky towards the camp. The sky's
            // own sun keeps following the sun below the horizon, so the sky darkens properly.
            light.transform.rotation = Quaternion.Euler(elevation, azimuth + 180f + _northYaw, 0f);
            light.color = color;
            light.intensity = Mathf.Max(0f, intensity);
            light.enabled = elevation > -2f && light.intensity > 0.001f;
        }
    }
}
