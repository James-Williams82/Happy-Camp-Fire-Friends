using UnityEngine;
using UnityEngine.Events;

namespace CampfireFriends.Construction
{
    /// <summary>Raises <see cref="Pressed"/> when a controller button goes down, e.g. the left menu button opening a menu.</summary>
    public class ControllerButtonEvent : MonoBehaviour
    {
        [SerializeField] private OVRInput.Button _button = OVRInput.Button.Start;
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.Active;

        public UnityEvent Pressed = new UnityEvent();

        private void Update()
        {
            if (OVRInput.GetDown(_button, _controller)) Pressed.Invoke();
        }
    }
}
