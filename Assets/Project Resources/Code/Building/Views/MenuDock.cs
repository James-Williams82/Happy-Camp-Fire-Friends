using System;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Keeps a world-space menu off to one side of the player, out of the way of pointing at the
    /// build. It stays put, so it can be looked at and pointed at, and only glides back to the
    /// player's side once they have turned or walked away from it.
    /// </summary>
    [Serializable]
    public class MenuDock
    {
        [Tooltip("Degrees around the player the docked menu sits; negative is left, out of the right hand's way.")]
        [Range(-90f, 90f)] public float Yaw = -45f;

        [Tooltip("The docked menu's size relative to its open size.")]
        [Range(0.3f, 1f)] public float Scale = 0.75f;

        [Tooltip("Re-dock once the menu is this many degrees from where the player faces.")]
        [Range(45f, 180f)] public float FollowAngle = 90f;

        [Tooltip("Re-dock once the menu is this many metres further than its docked distance.")]
        [Min(0.1f)] public float FollowSlack = 0.6f;

        [Tooltip("How quickly the menu glides; higher is snappier.")]
        [Min(1f)] public float GlideSpeed = 8f;

        private Vector3 _openScale = Vector3.one;
        private bool _known;
        private bool _gliding;

        public bool Docked { get; private set; }

        /// <summary>Remembers the menu's open size. Call before moving it.</summary>
        public void Init(Transform menu)
        {
            if (_known || menu == null) return;
            _openScale = menu.localScale;
            _known = true;
        }

        /// <summary>Puts the menu at the player's side, and keeps it there until <see cref="Stop"/>.</summary>
        public void Dock(Transform menu, ViewerPlacement placement)
        {
            Init(menu);
            Docked = true;
            _gliding = false;
            if (menu == null || !placement.HasViewer) return;

            var pose = placement.PoseAround(Yaw);
            menu.SetPositionAndRotation(pose.position, pose.rotation);
            menu.localScale = _openScale * Scale;
        }

        public void Stop(Transform menu)
        {
            Docked = false;
            _gliding = false;
            if (_known && menu != null) menu.localScale = _openScale;
        }

        public void Tick(Transform menu, ViewerPlacement placement, float deltaTime)
        {
            if (!Docked || menu == null || !placement.HasViewer) return;
            if (!_gliding && LeftBehind(menu, placement)) _gliding = true;
            if (!_gliding) return;

            var goal = placement.PoseAround(Yaw);
            var scale = _openScale * Scale;

            float t = 1f - Mathf.Exp(-GlideSpeed * deltaTime);
            menu.SetPositionAndRotation(Vector3.Lerp(menu.position, goal.position, t), Quaternion.Slerp(menu.rotation, goal.rotation, t));
            menu.localScale = Vector3.Lerp(menu.localScale, scale, t);

            if ((menu.position - goal.position).sqrMagnitude < 0.0001f && Quaternion.Angle(menu.rotation, goal.rotation) < 0.5f)
            {
                menu.SetPositionAndRotation(goal.position, goal.rotation);
                menu.localScale = scale;
                _gliding = false;
            }
        }

        /// <summary>Turned well away from the menu, or walked off from it.</summary>
        private bool LeftBehind(Transform menu, ViewerPlacement placement)
        {
            var offset = Vector3.ProjectOnPlane(menu.position - placement.Viewer.position, Vector3.up);
            if (offset.magnitude > placement.Distance + FollowSlack) return true;
            return Vector3.Angle(placement.LevelForward, offset) > FollowAngle;
        }
    }
}
