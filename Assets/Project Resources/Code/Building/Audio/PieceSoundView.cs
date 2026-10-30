using System.Collections.Generic;
using CampfireFriends.Building;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// One-shot sounds at a piece's position whenever the world changes: placed, removed, moved, or used.
    /// It listens to the world itself, so it plays for everyone's building, not just the local
    /// player's, and on clients exactly when the change arrives.
    ///
    /// A move reaches the world as a removal straight followed by an addition of the same id, so a
    /// removal waits a moment before it sounds, in case it turns out to be a move.
    /// </summary>
    public class PieceSoundView : MonoBehaviour
    {
        [SerializeField] private BuildSoundSet _sounds;

        [Tooltip("Template for the pooled one-shot voices: set spatial blend, rolloff and mixer group here.")]
        [SerializeField] private AudioSource _voiceTemplate;

        [Tooltip("How many piece sounds can overlap.")]
        [SerializeField, Min(1)] private int _voices = 6;

        [Tooltip("Seconds a removal waits to see whether the same piece comes straight back, which makes it a move.")]
        [SerializeField, Min(0f)] private float _moveWindow = 0.15f;

        [Tooltip("Seconds after joining a session during which arriving pieces are silent, so catching up on an existing camp doesn't clatter.")]
        [SerializeField, Min(0f)] private float _quietAfterJoin = 1f;

        private readonly Dictionary<int, PendingRemoval> _pendingRemovals = new Dictionary<int, PendingRemoval>();
        private readonly List<int> _expired = new List<int>();
        private AudioSource[] _pool = new AudioSource[0];
        private int _nextVoice;
        private IConstructionBackend _backend;
        private float _joinedAt;

        private struct PendingRemoval
        {
            public Vector3 Position;
            public PieceSize Size;
            public float Time;
        }

        private void Awake()
        {
            if (_voiceTemplate == null) return;

            _pool = new AudioSource[_voices];
            _pool[0] = _voiceTemplate;
            for (int i = 1; i < _voices; i++)
            {
                _pool[i] = Instantiate(_voiceTemplate, _voiceTemplate.transform.parent);
                _pool[i].name = $"{_voiceTemplate.name} {i + 1}";
            }
            foreach (var voice in _pool) voice.playOnAwake = false;
        }

        private void OnDisable() => Bind(null);

        private void Update()
        {
            var backend = ConstructionBackends.Current;
            if (backend != _backend) Bind(backend);

            FlushRemovals();
        }

        private void Bind(IConstructionBackend backend)
        {
            if (_backend != null)
            {
                _backend.World.Added -= OnAdded;
                _backend.World.Removed -= OnRemoved;
                _backend.World.StateChanged -= OnStateChanged;
            }

            _backend = backend;
            _pendingRemovals.Clear();
            if (_backend == null) return;

            _backend.World.Added += OnAdded;
            _backend.World.Removed += OnRemoved;
            _backend.World.StateChanged += OnStateChanged;
            _joinedAt = Time.time;
        }

        private void OnAdded(PlacedPiece piece)
        {
            bool moved = _pendingRemovals.Remove(piece.Id);
            if (!moved && Time.time - _joinedAt < _quietAfterJoin) return;

            Describe(piece, out var position, out var size);
            Play(moved ? _sounds.Move : _sounds.Place, size, position);
        }

        private void OnRemoved(PlacedPiece piece)
        {
            Describe(piece, out var position, out var size);
            _pendingRemovals[piece.Id] = new PendingRemoval { Position = position, Size = size, Time = Time.time };
        }

        private void OnStateChanged(PlacedPiece piece)
        {
            if (Time.time - _joinedAt < _quietAfterJoin) return;

            Describe(piece, out var position, out var size);
            Play(_sounds.Use, size, position);
        }

        private void FlushRemovals()
        {
            if (_pendingRemovals.Count == 0) return;

            _expired.Clear();
            foreach (var pair in _pendingRemovals)
            {
                if (Time.time - pair.Value.Time >= _moveWindow) _expired.Add(pair.Key);
            }

            foreach (int id in _expired)
            {
                var removal = _pendingRemovals[id];
                _pendingRemovals.Remove(id);
                Play(_sounds.Remove, removal.Size, removal.Position);
            }
        }

        private void Describe(PlacedPiece piece, out Vector3 position, out PieceSize size)
        {
            position = _backend.Grid.ToWorldBounds(piece.Bounds).center;
            size = _backend.Validator.TryGetDefinition(piece.PieceId, out var definition) ? definition.Size : PieceSize.Medium;
        }

        private void Play(SizedClips clips, PieceSize size, Vector3 position)
        {
            if (_sounds == null || _pool.Length == 0 || clips == null) return;

            var clip = clips.Pick(size);
            if (clip == null) return;

            var voice = _pool[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _pool.Length;

            voice.transform.position = position;
            voice.pitch = _sounds.RandomPitch();
            voice.PlayOneShot(clip);
        }
    }
}
