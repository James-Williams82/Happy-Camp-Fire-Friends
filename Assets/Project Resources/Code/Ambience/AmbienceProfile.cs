using System;
using UnityEngine;

namespace CampfireFriends.Ambience
{
    /// <summary>Where a scattered layer gets its sounds when it has no recordings.</summary>
    public enum ScatterSynth
    {
        None,
        Crickets,
    }

    /// <summary>
    /// The camp's soundscape: a wind bed, leaves, time-of-day beds and sparse scattered sounds.
    /// Every curve runs over the camp clock's 24 hours. Layers stay quiet and distant: together they
    /// should feel like a place, without any one of them being noticed.
    /// </summary>
    [CreateAssetMenu(menuName = "Campfire Friends/Ambience Profile", fileName = "AmbienceProfile")]
    public class AmbienceProfile : ScriptableObject
    {
        [Range(0f, 1f)] public float MasterVolume = 0.8f;

        public WindLayer Wind = new WindLayer();
        public RustleLayer Rustle = new RustleLayer();

        [Tooltip("Looping beds that fade with the time of day (birdsong, night insects). Silent until they have clips.")]
        public BedLayer[] Beds =
        {
            new BedLayer("dawn", Hours.Curve((0f, 0f), (4.5f, 0f), (5.5f, 0.35f), (7f, 0.35f), (8f, 0f), (24f, 0f))),
            new BedLayer("day", Hours.Curve((0f, 0f), (6f, 0f), (8f, 0.3f), (17.5f, 0.3f), (19.5f, 0f), (24f, 0f))),
            new BedLayer("night", Hours.Curve((0f, 0.3f), (5f, 0.3f), (6.5f, 0f), (19.5f, 0f), (21f, 0.3f), (24f, 0.3f))),
            MusicBed(),
        };

        /// <summary>The camp's old summer music, barely there: felt more than heard, fainter at night.</summary>
        public static BedLayer MusicBed() =>
            new BedLayer("music", Hours.Curve((0f, 0.025f), (6f, 0.025f), (8f, 0.05f), (18f, 0.05f), (20.5f, 0.025f), (24f, 0.025f)));

        [Tooltip("Single sounds placed at random around the player: crickets, a far bird, a branch creaking.")]
        public ScatterLayer[] Scatters =
        {
            new ScatterLayer("crickets", ScatterSynth.Crickets,
                Hours.Curve((0f, 1f), (4.5f, 0.8f), (6f, 0f), (19.5f, 0f), (21f, 0.8f), (24f, 1f)),
                interval: new Vector2(0.4f, 2.2f), distance: new Vector2(6f, 22f), height: new Vector2(-1.5f, -1f),
                volume: new Vector2(0.1f, 0.25f), pitch: new Vector2(0.92f, 1.08f)),
            new ScatterLayer("daycalls", ScatterSynth.None,
                Hours.Curve((0f, 0f), (5.5f, 0f), (7f, 1f), (11f, 0.6f), (17f, 0.7f), (19.5f, 0f), (24f, 0f)),
                interval: new Vector2(6f, 20f), distance: new Vector2(15f, 45f), height: new Vector2(2f, 8f),
                volume: new Vector2(0.15f, 0.35f), pitch: new Vector2(0.95f, 1.05f)),
            new ScatterLayer("nightcalls", ScatterSynth.None,
                Hours.Curve((0f, 1f), (4.5f, 0.6f), (6f, 0f), (20f, 0f), (21.5f, 1f), (24f, 1f)),
                interval: new Vector2(30f, 80f), distance: new Vector2(25f, 60f), height: new Vector2(0f, 10f),
                volume: new Vector2(0.2f, 0.4f), pitch: new Vector2(0.95f, 1.05f)),
            new ScatterLayer("creaks", ScatterSynth.None,
                Hours.Curve((0f, 0.8f), (8f, 0.2f), (18f, 0.3f), (22f, 0.8f), (24f, 0.8f)),
                interval: new Vector2(45f, 120f), distance: new Vector2(10f, 30f), height: new Vector2(0f, 4f),
                volume: new Vector2(0.15f, 0.3f), pitch: new Vector2(0.9f, 1.1f)),
        };
    }

    [Serializable]
    public class WindLayer
    {
        [Tooltip("Wind loops; with none, wind is generated.")]
        public AudioClip[] Clips = new AudioClip[0];

        public AnimationCurve Volume = Hours.Curve((0f, 0.36f), (5f, 0.3f), (8f, 0.14f), (14f, 0.15f), (19f, 0.24f), (21f, 0.36f), (24f, 0.36f));

        [Tooltip("0 is a steady breeze; 1 swells and gusts. Nights are gustier.")]
        public AnimationCurve Gustiness = Hours.Curve((0f, 0.8f), (7f, 0.4f), (13f, 0.5f), (20f, 0.75f), (24f, 0.8f));

        [Tooltip("Low-pass cutoff in calm air and at the height of a gust: gusts open up the sound.")]
        public float CalmCutoff = 350f;
        public float GustCutoff = 1600f;
    }

    [Serializable]
    public class RustleLayer
    {
        [Tooltip("Leaf loops; with none, leaves are generated. Loudness follows the wind's gusts.")]
        public AudioClip[] Clips = new AudioClip[0];

        public AnimationCurve Volume = Hours.Curve((0f, 0.1f), (12f, 0.07f), (24f, 0.1f));
    }

    [Serializable]
    public class BedLayer
    {
        [Tooltip("Recordings named amb_<id>_01, amb_<id>_02… are added by the build kit.")]
        public string Id;
        public AudioClip[] Clips = new AudioClip[0];
        public AnimationCurve Volume;

        public BedLayer(string id, AnimationCurve volume)
        {
            Id = id;
            Volume = volume;
        }
    }

    [Serializable]
    public class ScatterLayer
    {
        [Tooltip("Recordings named amb_<id>_01, amb_<id>_02… are added by the build kit.")]
        public string Id;

        [Tooltip("Generated sounds used while there are no recordings.")]
        public ScatterSynth Synth;
        public AudioClip[] Clips = new AudioClip[0];

        [Tooltip("0–1 over the day: how much this layer belongs. At 0 it is silent.")]
        public AnimationCurve Presence;

        [Tooltip("Seconds between sounds, at full presence.")]
        public Vector2 Interval;

        [Tooltip("Metres from the player, across the ground.")]
        public Vector2 Distance;

        [Tooltip("Metres above or below the player's eyes.")]
        public Vector2 Height;

        public Vector2 Volume;
        public Vector2 Pitch;

        public ScatterLayer(string id, ScatterSynth synth, AnimationCurve presence, Vector2 interval, Vector2 distance, Vector2 height, Vector2 volume, Vector2 pitch)
        {
            Id = id;
            Synth = synth;
            Presence = presence;
            Interval = interval;
            Distance = distance;
            Height = height;
            Volume = volume;
            Pitch = pitch;
        }
    }

    /// <summary>Curves over the camp clock, keyed in hours (0–24).</summary>
    public static class Hours
    {
        public static AnimationCurve Curve(params (float hour, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach (var (hour, value) in keys) curve.AddKey(new Keyframe(hour, value));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }
    }
}
