using UnityEngine;
using UnityEngine.Events;

namespace CampfireFriends.Story
{
    /// <summary>
    /// A place that reports when the local player walks into it: reaching the office, the shed,
    /// Cabin 6, the knock corridor. Shared zones raise a story fact; private ones (a footstep
    /// scare meant for one player) only fire <see cref="EnteredLocally"/> on that player's machine.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class StoryZone : StoryListener
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag RaisesFlag => (Night01Flag)_raises;
        private Night01Flag RequiresFlag => (Night01Flag)_requires;

        [SerializeField] private long _raises;
        [SerializeField] private long _requires;

        [Tooltip("Fires only here, for this player. Leave the flag at None for a private moment.")]
        public UnityEvent EnteredLocally = new UnityEvent();

        [SerializeField] private Transform _head;

        private BoxCollider _box;
        private bool _fired;

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
            if (_head == null && Camera.main != null) _head = Camera.main.transform;
        }

        protected override void OnFlag(Night01Flag flag, bool live) { }

        private void Update()
        {
            if (_fired || _head == null || Story == null || !Stories.Running) return;
            if (RequiresFlag != Night01Flag.None && !Story.State.Has(RequiresFlag)) return;
            if (RaisesFlag != Night01Flag.None && Story.State.Has(RaisesFlag)) { _fired = true; return; }
            if (!_box.bounds.Contains(_head.position)) return;

            _fired = true;
            EnteredLocally.Invoke();
            if (RaisesFlag != Night01Flag.None) Story.Raise(RaisesFlag);
        }

        public void SetHead(Transform head) => _head = head;
    }
}
