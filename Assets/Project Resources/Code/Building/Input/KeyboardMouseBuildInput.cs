using UnityEngine;
using UnityEngine.InputSystem;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Desktop test bindings: B enters or leaves build mode, 1–9 pick a piece, mouse wheel cycles it,
    /// R rotates, left click confirms, Esc steps back.
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

                frame.ToggleBuild = keyboard.bKey.wasPressedThisFrame;
                frame.Rotate = keyboard.rKey.wasPressedThisFrame;
                frame.Back = keyboard.escapeKey.wasPressedThisFrame;
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
