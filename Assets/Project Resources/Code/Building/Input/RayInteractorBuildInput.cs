using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Build input from an Interaction SDK controller rig. Confirm listens to the ray interactor's own
    /// selector (the trigger), so placing is a ray select like any other; face buttons drive the tool.
    /// Thumbsticks are left to locomotion.
    ///
    /// Defaults: aiming hand A rotates, B steps to the next piece; off hand X cycles
    /// off → place → delete, Y steps to the previous piece.
    /// </summary>
    public class RayInteractorBuildInput : BuildInputSource
    {
        [Tooltip("The ray interactor's selector, usually its TriggerSelector.")]
        [SerializeField, Interface(typeof(ISelector))] private Object _confirmSelector;

        [Tooltip("The aiming hand's controller (the ray interactor's ControllerRef).")]
        [SerializeField, Interface(typeof(IController))] private Object _mainHand;

        [Tooltip("The other hand's controller.")]
        [SerializeField, Interface(typeof(IController)), Optional] private Object _offHand;

        [Header("Aiming hand")]
        [SerializeField] private ControllerButtonUsage _rotate = ControllerButtonUsage.PrimaryButton;
        [SerializeField] private ControllerButtonUsage _nextPiece = ControllerButtonUsage.SecondaryButton;

        [Header("Off hand")]
        [SerializeField] private ControllerButtonUsage _cycleMode = ControllerButtonUsage.PrimaryButton;
        [SerializeField] private ControllerButtonUsage _previousPiece = ControllerButtonUsage.SecondaryButton;

        private ISelector _selector;
        private ButtonEdges _main;
        private ButtonEdges _off;
        private bool _confirmPressed;

        private void Awake()
        {
            _selector = _confirmSelector as ISelector;
            _main = new ButtonEdges(_mainHand as IController);
            _off = new ButtonEdges(_offHand as IController);
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

            frame.Rotate = _main.Pressed(_rotate);
            frame.CycleMode = _off.Pressed(_cycleMode);

            if (_main.Pressed(_nextPiece)) frame.Cycle = 1;
            else if (_off.Pressed(_previousPiece)) frame.Cycle = -1;

            _main.EndFrame();
            _off.EndFrame();
            return frame;
        }

        /// <summary>Turns a controller's held buttons into press edges, polled once per frame.</summary>
        private sealed class ButtonEdges
        {
            private readonly IController _controller;
            private ControllerButtonUsage _held;
            private ControllerButtonUsage _now;

            public ButtonEdges(IController controller) => _controller = controller;

            public bool Pressed(ControllerButtonUsage usage)
            {
                if (usage == ControllerButtonUsage.None || _controller == null || !_controller.IsConnected) return false;

                bool down = _controller.IsButtonUsageAnyActive(usage);
                if (down) _now |= usage;
                return down && (_held & usage) == 0;
            }

            public void EndFrame()
            {
                _held = _now;
                _now = ControllerButtonUsage.None;
            }
        }
    }
}
