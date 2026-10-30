using System.Collections.Generic;
using CampfireFriends.Shifts;
using UnityEngine;

namespace CampfireFriends.Ambience
{
    /// <summary>
    /// Plays the camp's soundscape for this player, following the shared shift clock: wind that
    /// swells and gusts (gustier at night), leaves that answer the gusts, beds for dawn, day and
    /// night, and sparse sounds scattered around the listener. Everyone hears the same time of day;
    /// the exact sounds are each player's own, which is fine for atmosphere.
    /// </summary>
    public class AmbienceController : MonoBehaviour
    {
        [SerializeField] private AmbienceProfile _profile;

        [Tooltip("The player's head. Scattered sounds are placed around it.")]
        [SerializeField] private Transform _listener;

        [Tooltip("Voices for scattered sounds that can overlap.")]
        [SerializeField, Range(2, 16)] private int _voices = 8;

        [Tooltip("Heard while no shift clock is running.")]
        [SerializeField, Range(0f, 24f)] private float _previewHour = 22f;

        private AudioSource _wind;
        private AudioLowPassFilter _windFilter;
        private AudioSource _rustle;
        private AudioSource[] _beds = new AudioSource[0];
        private AudioSource[] _pool = new AudioSource[0];
        private int _nextVoice;

        private ScatterTimer[] _timers = new ScatterTimer[0];
        private AudioClip[][] _scatterClips = new AudioClip[0][];
        private readonly List<AudioClip> _generated = new List<AudioClip>();
        private GustModel _gusts;

        private void Start()
        {
            if (_profile == null) return;
            if (_listener == null && Camera.main != null) _listener = Camera.main.transform;

            int seed = Random.Range(1, int.MaxValue);
            _gusts = new GustModel(seed);

            _wind = Loop("Wind", Pick(_profile.Wind.Clips) ?? Generate(ProceduralSounds.Wind(seed)));
            _windFilter = _wind.gameObject.AddComponent<AudioLowPassFilter>();
            _rustle = Loop("Leaves", Pick(_profile.Rustle.Clips) ?? Generate(ProceduralSounds.Rustle(seed + 1)));

            _beds = new AudioSource[_profile.Beds.Length];
            for (int i = 0; i < _beds.Length; i++)
            {
                var clip = Pick(_profile.Beds[i].Clips);
                if (clip != null) _beds[i] = Loop($"Bed {_profile.Beds[i].Id}", clip);
            }

            _timers = new ScatterTimer[_profile.Scatters.Length];
            _scatterClips = new AudioClip[_profile.Scatters.Length][];
            for (int i = 0; i < _timers.Length; i++)
            {
                var layer = _profile.Scatters[i];
                _timers[i] = new ScatterTimer(layer.Interval.x, layer.Interval.y, seed + 10 + i);
                _scatterClips[i] = layer.Clips.Length > 0 ? layer.Clips : Synthesize(layer.Synth, seed + 20 + i);
            }

            _pool = new AudioSource[_voices];
            for (int i = 0; i < _pool.Length; i++) _pool[i] = Voice(i);
        }

        private void OnDestroy()
        {
            foreach (var clip in _generated) Destroy(clip);
        }

        private void Update()
        {
            if (_profile == null || _wind == null) return;

            var clock = ShiftClocks.Current?.Clock;
            float hour = clock != null ? clock.Hours : _previewHour;
            float master = _profile.MasterVolume;

            float gust = _gusts.Strength(Time.time, Mathf.Clamp01(_profile.Wind.Gustiness.Evaluate(hour)));
            _wind.volume = master * Mathf.Max(0f, _profile.Wind.Volume.Evaluate(hour)) * (0.25f + 0.75f * gust);
            _windFilter.cutoffFrequency = Mathf.Lerp(_profile.Wind.CalmCutoff, _profile.Wind.GustCutoff, gust * gust);
            _rustle.volume = master * Mathf.Max(0f, _profile.Rustle.Volume.Evaluate(hour)) * gust * gust;

            for (int i = 0; i < _beds.Length; i++)
                if (_beds[i] != null) _beds[i].volume = master * Mathf.Max(0f, _profile.Beds[i].Volume.Evaluate(hour));

            for (int i = 0; i < _timers.Length; i++)
            {
                var layer = _profile.Scatters[i];
                float presence = Mathf.Clamp01(layer.Presence.Evaluate(hour));
                if (_timers[i].Tick(Time.deltaTime, presence) && _scatterClips[i].Length > 0)
                    PlayScattered(layer, _scatterClips[i], _timers[i], master);
            }
        }

        /// <summary>One sound somewhere around the listener: a random bearing, distance and height.</summary>
        private void PlayScattered(ScatterLayer layer, AudioClip[] clips, ScatterTimer random, float master)
        {
            if (_listener == null || _pool.Length == 0) return;

            float bearing = random.Next() * 360f;
            float distance = Mathf.Lerp(layer.Distance.x, layer.Distance.y, random.Next());
            float height = Mathf.Lerp(layer.Height.x, layer.Height.y, random.Next());
            var direction = Quaternion.Euler(0f, bearing, 0f) * Vector3.forward;

            var voice = _pool[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _pool.Length;

            voice.transform.position = _listener.position + direction * distance + Vector3.up * height;
            voice.clip = clips[Mathf.Min(clips.Length - 1, (int)(random.Next() * clips.Length))];
            voice.pitch = Mathf.Lerp(layer.Pitch.x, layer.Pitch.y, random.Next());
            voice.volume = master * Mathf.Lerp(layer.Volume.x, layer.Volume.y, random.Next());
            voice.Play();
        }

        private AudioClip[] Synthesize(ScatterSynth synth, int seed)
        {
            if (synth != ScatterSynth.Crickets) return new AudioClip[0];

            var chirps = ProceduralSounds.CricketChirps(seed);
            _generated.AddRange(chirps);
            return chirps;
        }

        private AudioClip Generate(AudioClip clip)
        {
            _generated.Add(clip);
            return clip;
        }

        private static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>A looping bed heard everywhere (not placed in the world), starting at a random point.</summary>
        private AudioSource Loop(string name, AudioClip clip)
        {
            var source = new GameObject(name).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.time = Random.Range(0f, clip.length);
            source.Play();
            return source;
        }

        /// <summary>A placed voice for scattered sounds: spatialised, fading with distance.</summary>
        private AudioSource Voice(int index)
        {
            var source = new GameObject($"Scattered voice {index + 1}").AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.spatialize = true;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 4f;
            source.maxDistance = 80f;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
