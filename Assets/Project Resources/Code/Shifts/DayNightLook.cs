using UnityEngine;

namespace CampfireFriends.Shifts
{
    /// <summary>
    /// How the camp looks through the day, keyed by the camp clock: every gradient and curve runs
    /// from midnight (0) through noon (0.5) to midnight (1). Night is dark enough to need lights but
    /// never black, so the camp stays readable in a headset.
    /// </summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Day Night Look", fileName = "DayNightLook")]
    public class DayNightLook : ScriptableObject
    {
        [Header("Sun")]
        public Gradient SunColor = Keys(
            (0.27f, Rgb(1f, 0.45f, 0.2f)), (0.33f, Rgb(1f, 0.85f, 0.7f)), (0.5f, Rgb(1f, 0.97f, 0.9f)),
            (0.7f, Rgb(1f, 0.85f, 0.65f)), (0.8f, Rgb(1f, 0.4f, 0.15f)));
        public AnimationCurve SunIntensity = Curve((0f, 0f), (0.26f, 0f), (0.3f, 0.6f), (0.4f, 1.2f), (0.5f, 1.3f), (0.7f, 1.1f), (0.79f, 0.4f), (0.82f, 0f), (1f, 0f));

        [Header("Moon")]
        public Color MoonColor = Rgb(0.55f, 0.65f, 1f);
        public AnimationCurve MoonIntensity = Curve((0f, 0.18f), (0.22f, 0.15f), (0.28f, 0f), (0.8f, 0f), (0.87f, 0.15f), (1f, 0.18f));

        [Header("Ambient (sky, horizon, ground)")]
        public Gradient AmbientSky = Keys(
            (0f, Rgb(0.03f, 0.04f, 0.08f)), (0.26f, Rgb(0.08f, 0.08f, 0.14f)), (0.33f, Rgb(0.45f, 0.5f, 0.6f)),
            (0.5f, Rgb(0.55f, 0.62f, 0.72f)), (0.75f, Rgb(0.45f, 0.42f, 0.5f)), (0.83f, Rgb(0.08f, 0.08f, 0.15f)), (1f, Rgb(0.03f, 0.04f, 0.08f)));
        public Gradient AmbientEquator = Keys(
            (0f, Rgb(0.02f, 0.025f, 0.05f)), (0.27f, Rgb(0.2f, 0.12f, 0.12f)), (0.35f, Rgb(0.4f, 0.4f, 0.4f)),
            (0.5f, Rgb(0.45f, 0.45f, 0.45f)), (0.77f, Rgb(0.45f, 0.25f, 0.18f)), (0.84f, Rgb(0.03f, 0.03f, 0.06f)), (1f, Rgb(0.02f, 0.025f, 0.05f)));
        public Gradient AmbientGround = Keys(
            (0f, Rgb(0.01f, 0.01f, 0.02f)), (0.3f, Rgb(0.12f, 0.1f, 0.08f)), (0.5f, Rgb(0.22f, 0.2f, 0.17f)),
            (0.8f, Rgb(0.08f, 0.06f, 0.05f)), (1f, Rgb(0.01f, 0.01f, 0.02f)));

        [Header("Fog")]
        public Gradient FogColor = Keys(
            (0f, Rgb(0.02f, 0.025f, 0.05f)), (0.27f, Rgb(0.35f, 0.25f, 0.25f)), (0.35f, Rgb(0.65f, 0.7f, 0.75f)),
            (0.5f, Rgb(0.7f, 0.76f, 0.82f)), (0.77f, Rgb(0.6f, 0.38f, 0.28f)), (0.85f, Rgb(0.03f, 0.035f, 0.07f)), (1f, Rgb(0.02f, 0.025f, 0.05f)));
        public AnimationCurve FogDensity = Curve((0f, 0.035f), (0.25f, 0.03f), (0.35f, 0.006f), (0.5f, 0.003f), (0.75f, 0.008f), (0.87f, 0.03f), (1f, 0.035f));

        [Header("Sky")]
        [Tooltip("Skybox exposure: bright by day, nearly out at night.")]
        public AnimationCurve SkyExposure = Curve((0f, 0.08f), (0.25f, 0.1f), (0.32f, 0.9f), (0.5f, 1.3f), (0.75f, 1f), (0.84f, 0.12f), (1f, 0.08f));

        public static float Key(float hour) => Mathf.Repeat(hour, 24f) / 24f;

        private static Color Rgb(float r, float g, float b) => new Color(r, g, b);

        private static Gradient Keys(params (float time, Color color)[] keys)
        {
            var colors = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++) colors[i] = new GradientColorKey(keys[i].color, keys[i].time);

            var gradient = new Gradient();
            gradient.SetKeys(colors, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static AnimationCurve Curve(params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach (var (time, value) in keys) curve.AddKey(new Keyframe(time, value));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }
    }
}
