using System;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Where a world-space menu appears when it opens: level in front of the player's eyes, further
    /// away with controllers (pointed at by ray) than with hands (within reach to poke). A yaw turns
    /// that spot around the player, e.g. off to one side.
    /// </summary>
    [Serializable]
    public class ViewerPlacement
    {
        [Tooltip("The player's head. Without it the menu stays where it is.")]
        public Transform Viewer;

        [Tooltip("Hands or controllers; none means controllers.")]
        public InputModalitySource Modality;

        [Tooltip("Metres in front of the eyes with controllers.")]
        [Min(0.2f)] public float ControllerDistance = 1f;
        public float ControllerHeightBelowEyes = 0.25f;

        [Tooltip("Metres in front of the eyes with hands.")]
        [Min(0.2f)] public float HandDistance = 0.45f;
        public float HandHeightBelowEyes = 0.15f;

        public bool HasViewer => Viewer != null;

        /// <summary>
        /// The headset has reported a pose. Until then the head sits at the rig's origin on the
        /// floor, so a menu placed in front of it would land in the ground.
        /// </summary>
        public bool ViewerTracked => Viewer != null && Viewer.localPosition.sqrMagnitude > 1e-6f;

        /// <summary>How far from the eyes the menu sits for the current input.</summary>
        public float Distance => Modality != null && Modality.UsingHands ? HandDistance : ControllerDistance;

        /// <summary>The viewer's facing, flattened to the ground.</summary>
        public Vector3 LevelForward
        {
            get
            {
                if (Viewer == null) return Vector3.forward;
                var forward = Vector3.ProjectOnPlane(Viewer.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.0001f) forward = Vector3.ProjectOnPlane(Viewer.up, Vector3.up);
                return forward.normalized;
            }
        }

        public void PlaceInFront(Transform menu)
        {
            if (Viewer == null || menu == null) return;

            var pose = PoseAround(0f);
            menu.SetPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>The menu's pose turned <paramref name="yawDegrees"/> about the player (negative is left), facing them.</summary>
        public Pose PoseAround(float yawDegrees)
        {
            bool hands = Modality != null && Modality.UsingHands;
            float drop = hands ? HandHeightBelowEyes : ControllerHeightBelowEyes;

            var direction = Quaternion.AngleAxis(yawDegrees, Vector3.up) * LevelForward;
            var origin = Viewer != null ? Viewer.position : Vector3.zero;
            return new Pose(origin + direction * Distance + Vector3.down * drop, Quaternion.LookRotation(direction, Vector3.up));
        }
    }
}
