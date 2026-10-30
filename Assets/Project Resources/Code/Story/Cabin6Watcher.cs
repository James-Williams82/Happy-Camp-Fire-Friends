using CampfireFriends.Building;
using CampfireFriends.Construction;
using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Watches the build around Cabin 6's doorway and reports how it's going: the first board, the
    /// opening secured (coverage, not a fixed layout), and over-building. Runs wherever the story is
    /// decided; reports are facts, so a guest's copy reporting too changes nothing.
    /// </summary>
    public class Cabin6Watcher : StoryListener
    {
        [Tooltip("The doorway's cells: lowest cell, exclusive upper cell, and a unit step through the door.")]
        [SerializeField] private Vector3Int _min = new Vector3Int(-6, 0, 52);
        [SerializeField] private Vector3Int _max = new Vector3Int(-5, 4, 54);
        [SerializeField] private Vector3Int _through = new Vector3Int(1, 0, 0);

        [SerializeField, Range(0.5f, 1f)] private float _securedAt = 0.85f;
        [SerializeField, Min(2)] private int _extraPieces = 5;

        private IConstructionBackend _backend;
        private OpeningCoverage _coverage;

        private void Awake() => _coverage = new OpeningCoverage(_min, _max, _through);

        protected override void OnFlag(Night01Flag flag, bool live) { }

        private void Update()
        {
            var backend = ConstructionBackends.Current;
            if (backend == _backend) return;
            if (_backend != null) { _backend.World.Added -= OnChanged; _backend.World.Removed -= OnChanged; }
            _backend = backend;
            if (_backend != null) { _backend.World.Added += OnChanged; _backend.World.Removed += OnChanged; }
        }

        private void OnDestroy()
        {
            if (_backend != null) { _backend.World.Added -= OnChanged; _backend.World.Removed -= OnChanged; }
        }

        private void OnChanged(PlacedPiece _)
        {
            if (Story == null || !Stories.Running || _backend == null) return;

            float covered = _coverage.Covered(_backend.World, out int pieces);
            if (pieces > 0) Story.Raise(Night01Flag.Cabin06FirstBoard);
            if (covered >= _securedAt) Story.Raise(Night01Flag.Cabin06Secured);
            if (pieces >= _extraPieces) Story.Raise(Night01Flag.Cabin06ExtraCover);
        }
    }
}
