using UnityEngine;

namespace CampfireFriends.Locomotion
{
    /// <summary>
    /// The camp map, pulled up with a click of the left stick: it opens in front of the guard at
    /// chest height with a dot where they're standing, and comes along as they walk and turn,
    /// gliding after the head rather than locked to it. Click again to put it away.
    /// The image maps to the camp at <see cref="_metresPerPixel"/> around <see cref="_originPixel"/>
    /// (the main gate), as the camp was blocked out from it.
    /// </summary>
    public class FieldMap : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [Tooltip("What opens and closes: the map image and the checklist beside it.")]
        [SerializeField] private Transform _board;
        [Tooltip("The dot, a child of the map image (whose local x/y span the image from -0.5 to 0.5).")]
        [SerializeField] private Transform _youAreHere;

        [SerializeField] private OVRInput.Button _toggle = OVRInput.Button.PrimaryThumbstick;
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.LTouch;

        [Header("The map image")]
        [SerializeField] private Vector2 _imagePixels = new Vector2(1536f, 1024f);
        [SerializeField] private Vector2 _originPixel = new Vector2(565f, 880f);
        [SerializeField] private float _metresPerPixel = 0.12f;

        [Header("Placement")]
        [SerializeField] private float _ahead = 0.5f;
        [SerializeField] private float _below = 0.22f;
        [Tooltip("How quickly the map catches up with the head; higher is snappier.")]
        [SerializeField, Min(0.5f)] private float _follow = 4f;

        public bool Open => _board != null && _board.gameObject.activeSelf;

        private void Start()
        {
            if (_board != null) _board.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_head == null || _board == null) return;
            if (OVRInput.GetDown(_toggle, _controller)) Show(!Open);
            if (!Open) return;

            var goal = Held();
            float t = 1f - Mathf.Exp(-_follow * Time.deltaTime);
            _board.SetPositionAndRotation(Vector3.Lerp(_board.position, goal.position, t), Quaternion.Slerp(_board.rotation, goal.rotation, t));
            Mark();
        }

        private void Show(bool open)
        {
            _board.gameObject.SetActive(open);
            if (!open) return;

            var held = Held();
            _board.SetPositionAndRotation(held.position, held.rotation);
            Mark();
        }

        /// <summary>Where the map is held: ahead of the head at chest height, facing the eyes, tipped back.</summary>
        private Pose Held()
        {
            var forward = Flat(_head.forward);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            var position = _head.position + forward.normalized * _ahead - Vector3.up * _below;
            return new Pose(position, Quaternion.LookRotation(position - _head.position, Vector3.up));
        }

        private void Mark()
        {
            if (_youAreHere == null) return;
            var at = _head.position;
            var pixel = new Vector2(_originPixel.x + at.x / _metresPerPixel, _originPixel.y - at.z / _metresPerPixel);
            var uv = new Vector2(pixel.x / _imagePixels.x, 1f - pixel.y / _imagePixels.y);
            bool onMap = uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
            _youAreHere.gameObject.SetActive(onMap);
            if (onMap) _youAreHere.localPosition = new Vector3(uv.x - 0.5f, uv.y - 0.5f, _youAreHere.localPosition.z);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
