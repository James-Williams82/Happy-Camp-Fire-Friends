using Fusion;
using UnityEngine;

namespace CampfireFriends.Network
{
    /// <summary>
    /// A player as others see them: a head and two hands that follow the player's tracked pose.
    /// The pose arrives as input, the host replicates it, and every peer shows it. Players don't
    /// see their own avatar; they have their real hands and view.
    /// </summary>
    public class PlayerAvatar : NetworkBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;

        [Tooltip("One tint per player slot, so players can tell each other apart.")]
        [SerializeField] private Color[] _tints =
        {
            new Color(0.95f, 0.55f, 0.2f),
            new Color(0.3f, 0.65f, 0.95f),
            new Color(0.45f, 0.85f, 0.4f),
            new Color(0.9f, 0.4f, 0.75f),
        };

        [Networked] private Vector3 HeadPosition { get; set; }
        [Networked] private Quaternion HeadRotation { get; set; }
        [Networked] private Vector3 LeftPosition { get; set; }
        [Networked] private Quaternion LeftRotation { get; set; }
        [Networked] private Vector3 RightPosition { get; set; }
        [Networked] private Quaternion RightRotation { get; set; }

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public override void Spawned()
        {
            name = $"Avatar (player {Object.InputAuthority.PlayerId})";

            var renderers = GetComponentsInChildren<Renderer>(true);
            if (HasInputAuthority)
            {
                foreach (var r in renderers) r.enabled = false;
                return;
            }

            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, _tints.Length > 0 ? _tints[Mathf.Abs(Object.InputAuthority.PlayerId) % _tints.Length] : Color.white);
            foreach (var r in renderers) r.SetPropertyBlock(block);
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out AvatarInput input)) return;

            HeadPosition = input.HeadPosition;
            HeadRotation = input.HeadRotation;
            LeftPosition = input.LeftPosition;
            LeftRotation = input.LeftRotation;
            RightPosition = input.RightPosition;
            RightRotation = input.RightRotation;
        }

        public override void Render()
        {
            if (HasInputAuthority) return;

            Apply(_head, HeadPosition, HeadRotation);
            Apply(_leftHand, LeftPosition, LeftRotation);
            Apply(_rightHand, RightPosition, RightRotation);
        }

        private static void Apply(Transform part, Vector3 position, Quaternion rotation)
        {
            if (part == null) return;
            if (rotation.x == 0f && rotation.y == 0f && rotation.z == 0f && rotation.w == 0f) rotation = Quaternion.identity;
            part.SetPositionAndRotation(position, rotation);
        }
    }
}
