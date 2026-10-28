using UnityEngine;
using UnityEngine.InputSystem;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Editor/desktop test camera for the build sandbox: hold right mouse to look, WASD to move,
    /// Space/C up and down, Shift to go faster.
    /// </summary>
    public class DesktopFlyCamera : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 4f;
        [SerializeField, Min(1f)] private float _fastMultiplier = 3f;
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.15f;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            var euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue() * _lookSensitivity;
                _yaw += delta.x;
                _pitch = Mathf.Clamp(_pitch - delta.y, -89f, 89f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            var move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += transform.forward;
            if (keyboard.sKey.isPressed) move -= transform.forward;
            if (keyboard.dKey.isPressed) move += transform.right;
            if (keyboard.aKey.isPressed) move -= transform.right;
            if (keyboard.spaceKey.isPressed) move += Vector3.up;
            if (keyboard.cKey.isPressed) move -= Vector3.up;

            float speed = _speed * (keyboard.leftShiftKey.isPressed ? _fastMultiplier : 1f);
            transform.position += move * (speed * Time.deltaTime);
        }
    }
}
