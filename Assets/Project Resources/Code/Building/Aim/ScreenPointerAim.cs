using UnityEngine;
using UnityEngine.InputSystem;

namespace CampfireFriends.Construction
{
    /// <summary>Aims through the mouse cursor, or the screen centre while the cursor is locked.</summary>
    public class ScreenPointerAim : AimSource
    {
        [Tooltip("Defaults to the main camera.")]
        [SerializeField] private Camera _camera;

        public override bool TryGetRay(out Ray ray)
        {
            var cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
            {
                ray = default;
                return false;
            }

            var mouse = Mouse.current;
            Vector2 screen = Cursor.lockState == CursorLockMode.Locked || mouse == null
                ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                : mouse.position.ReadValue();

            ray = cam.ScreenPointToRay(screen);
            return true;
        }
    }
}
