using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Build input from an Interaction SDK controller rig. Confirm listens to the ray interactor's own
    /// selector (the trigger), so placing and selecting are ray selects like any other; face buttons
    /// drive the tool. Pieces are picked from the build menu, and the rest of the thumbstick is left
    /// to locomotion.
    ///
    /// Defaults on the aiming hand: thumbstick click enters or leaves build mode, A rotates, B steps back.
    /// </summary>
    public class RayInteractorBuildInput : BuildInputSource
    {
        [Tooltip("The ray interactor's selector, usually its TriggerSelector.")]
        [SerializeField, Interface(typeof(ISelector))] private Object _confirmSelector;

        [Tooltip("The aiming hand's controller (the ray interactor's ControllerRef).")]
        [SerializeField, Interface(typeof(IController))] private Object _mainHand;

        [SerializeField] private ControllerButtonUsage _toggleBuild = ControllerButtonUsage.Primary2DAxisClick;
        [SerializeField] private ControllerButtonUsage _rotate = ControllerButtonUsage.PrimaryButton;
        [SerializeField] private ControllerButtonUsage _back = ControllerButtonUsage.SecondaryButton;

        private ISelector _selector;
        private IController _controller;
        private ControllerButtonUsage _held;
        private bool _confirmPressed;

        private void Awake()
        {
            _selector = _confirmSelector as ISelector;
            _controller = _mainHand as IController;
        }

        private void OnEnable()
        {
            if (_selector != null) _selector.WhenSelected += OnSelected;
        }

        private void OnDisable()
        {
            if (_selector != null) _selector.WhenSelected -= OnSelected;
            _confirmPressed = false;
        }

        private void OnSelected() => _confirmPressed = true;

        public override BuildInputFrame Poll()
        {
            var frame = BuildInputFrame.Empty;

            frame.Confirm = _confirmPressed;
            _confirmPressed = false;

            var down = PressedThisFrame();
            frame.ToggleBuild = (down & _toggleBuild) != 0;
            frame.Rotate = (down & _rotate) != 0;
            frame.Back = (down & _back) != 0;
            return frame;
        }

        /// <summary>The bound buttons that went down since the last poll.</summary>
        private ControllerButtonUsage PressedThisFrame()
        {
            var now = ControllerButtonUsage.None;
            if (_controller != null && _controller.IsConnected)
            {
                foreach (var usage in new[] { _toggleBuild, _rotate, _back })
                {
                    if (usage != ControllerButtonUsage.None && _controller.IsButtonUsageAnyActive(usage)) now |= usage;
                }
            }

            var down = now & ~_held;
            _held = now;
            return down;
        }
    }
}
