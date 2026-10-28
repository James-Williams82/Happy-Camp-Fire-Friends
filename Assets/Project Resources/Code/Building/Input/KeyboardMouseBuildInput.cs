using UnityEngine;
using UnityEngine.InputSystem;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Desktop test bindings from doc §13.3: 1–9 select, mouse wheel cycles, R rotates,
    /// left click confirms, X toggles delete, Esc cancels.
    /// </summary>
    public class KeyboardMouseBuildInput : BuildInputSource
    {
        public override BuildInputFrame Poll()
        {
            var frame = BuildInputFrame.Empty;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame) frame.Slot = i;
                }

                frame.Rotate = keyboard.rKey.wasPressedThisFrame;
                frame.ToggleDelete = keyboard.xKey.wasPressedThisFrame;
                frame.Cancel = keyboard.escapeKey.wasPressedThisFrame;
            }

            if (mouse != null)
            {
                frame.Confirm = mouse.leftButton.wasPressedThisFrame;

                float scroll = mouse.scroll.ReadValue().y;
                frame.Cycle = scroll > 0f ? 1 : scroll < 0f ? -1 : 0;
            }

            return frame;
        }
    }
}
