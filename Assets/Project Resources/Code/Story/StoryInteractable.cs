using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Something in the camp to point at and pull the trigger on: take the fuse, pour the fuel,
    /// pull the starter, check a latch. Reports its fact to the story, once. A small label names
    /// what it does, or what it still needs ("Needs fuel"): while the ray is on it, and whenever
    /// the guard is close and it isn't done yet, so a room's jobs can be seen at a glance.
    /// </summary>
    public class StoryInteractable : StoryListener
    {
        // Stored as bits: Night 1 has more facts than an int-sized enum holds.
        private Night01Flag RaisesFlag => (Night01Flag)_raises;
        private Night01Flag RequiresFlag => (Night01Flag)_requires;

        [SerializeField, Interface(typeof(IInteractableView))] private Object _interactable;

        [Tooltip("What this does, shown under the ray.")]
        [SerializeField] private string _verb = "Inspect";
        [SerializeField] private long _raises;

        [Tooltip("Facts that must be true first (all of them).")]
        [SerializeField] private long _requires;
        [SerializeField] private string _whenLocked = "Not yet";

        [SerializeField] private TMP_Text _label;

        [Tooltip("Hidden once done (the fuse taken from the cabinet).")]
        [SerializeField] private GameObject[] _hideWhenDone = new GameObject[0];

        [Tooltip("For this player only (equipping their own flashlight).")]
        public UnityEvent UsedLocally = new UnityEvent();

        [Tooltip("Can be used again by each player even once the fact is known (flashlights: one each).")]
        [SerializeField] private bool _repeatable;

        [Tooltip("The label also shows, until it's done, while the guard is this close (metres). 0: only under the ray.")]
        [SerializeField, Min(0f)] private float _showWithin = 4f;

        private IInteractableView View => _interactable as IInteractableView;
        private bool _usedHere;
        private bool _hovered;
        private Transform _viewer;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (View != null) View.WhenStateChanged += OnStateChanged;
            _hovered = false;
            ShowLabel(false);
        }

        protected override void OnDisable()
        {
            if (View != null) View.WhenStateChanged -= OnStateChanged;
            base.OnDisable();
        }

        protected override void OnFlag(Night01Flag flag, bool live)
        {
            if (flag == RaisesFlag && !_repeatable)
                foreach (var go in _hideWhenDone) if (go != null) go.SetActive(false);
        }

        private bool Ready => Story != null && (RequiresFlag == Night01Flag.None || Story.State.Has(RequiresFlag));
        private bool Done => Story != null && Story.State.Has(RaisesFlag) && (!_repeatable || _usedHere);

        private void OnStateChanged(InteractableStateChangeArgs args)
        {
            _hovered = args.NewState != InteractableState.Normal;
            ShowLabel(_hovered);

            if (args.PreviousState != InteractableState.Select || args.NewState == InteractableState.Select) return;
            if (Story == null || !Stories.Running || !Ready || Done) return;

            _usedHere = true;
            UsedLocally.Invoke();
            Story.Raise(RaisesFlag);
            ShowLabel(true);
        }

        private void Update()
        {
            if (_label == null) return;
            if (_viewer == null && Camera.main != null) _viewer = Camera.main.transform;
            bool near = _showWithin > 0f && _viewer != null && !Done
                        && (_viewer.position - _label.transform.position).sqrMagnitude < _showWithin * _showWithin;
            ShowLabel(_hovered || near);
        }

        private void ShowLabel(bool show)
        {
            if (_label == null) return;
            show &= Stories.Running;
            if (_label.gameObject.activeSelf != show) _label.gameObject.SetActive(show);
            if (!show) return;
            var text = Done ? "Done" : Ready ? _verb : _whenLocked;
            if (_label.text != text) _label.text = text;
        }
    }
}
