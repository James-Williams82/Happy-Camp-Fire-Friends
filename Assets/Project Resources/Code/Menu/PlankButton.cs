using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CampfireFriends.Menu
{
    /// <summary>
    /// A wooden plank you point at and pull the trigger on: a 3D button for signposts and props.
    /// It brightens under the ray, darkens while pressed, and fires <see cref="Clicked"/> on release.
    /// The interactable (a ray interactable on the plank's collider) does the pointing.
    /// </summary>
    public class PlankButton : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractableView))] private Object _interactable;
        [SerializeField] private Renderer _plank;
        [SerializeField] private TMP_Text _label;

        [SerializeField] private Color _normal = new Color(0.42f, 0.3f, 0.18f);
        [SerializeField] private Color _hover = new Color(0.62f, 0.46f, 0.26f);
        [SerializeField] private Color _pressed = new Color(0.3f, 0.2f, 0.12f);
        [SerializeField] private Color _disabled = new Color(0.22f, 0.18f, 0.15f);

        public UnityEvent Clicked = new UnityEvent();

        private IInteractableView View => _interactable as IInteractableView;
        private MaterialPropertyBlock _block;
        private bool _enabled = true;

        public string Text
        {
            get => _label != null ? _label.text : "";
            set { if (_label != null) _label.text = value; }
        }

        /// <summary>Greyed out and ignoring presses when false (e.g. "coming soon").</summary>
        public bool Interactable
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Paint(View != null ? View.State : InteractableState.Normal);
            }
        }

        private void OnEnable()
        {
            if (View != null) View.WhenStateChanged += OnStateChanged;
            Paint(View != null ? View.State : InteractableState.Normal);
        }

        private void OnDisable()
        {
            if (View != null) View.WhenStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(InteractableStateChangeArgs args)
        {
            Paint(args.NewState);
            if (_enabled && args.PreviousState == InteractableState.Select && args.NewState != InteractableState.Select)
                Clicked.Invoke();
        }

        private void Paint(InteractableState state)
        {
            if (_plank == null) return;
            _block ??= new MaterialPropertyBlock();

            var colour = !_enabled ? _disabled
                : state == InteractableState.Select ? _pressed
                : state == InteractableState.Hover ? _hover
                : _normal;
            _plank.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", colour);
            _plank.SetPropertyBlock(_block);
            if (_label != null) _label.alpha = _enabled ? 1f : 0.45f;
        }
    }
}
