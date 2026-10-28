using CampfireFriends.Building;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The small menu beside a selected piece: rotate, reposition, delete, close. It forwards clicks
    /// to the build controller as commands and keeps no state of its own. It shows only while a piece
    /// is selected, standing off the piece's lower corner on the viewer's right, facing the viewer.
    /// </summary>
    public class PieceModifyMenu : MonoBehaviour
    {
        [SerializeField] private BuildController _controller;

        [Header("Buttons")]
        [SerializeField] private Button _rotate;
        [SerializeField] private Button _reposition;
        [SerializeField] private Button _delete;
        [SerializeField] private Button _close;

        [Header("Presentation")]
        [Tooltip("The visible panel. A child, so this component keeps listening while it's hidden.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("The player's head; the menu faces it and stays within reach of it.")]
        [SerializeField] private Transform _viewer;

        [Tooltip("Metres the menu stands in front of the piece's near side.")]
        [SerializeField, Min(0f)] private float _standOff = 0.25f;

        [Tooltip("Metres. A piece further away than this still gets its menu at this distance.")]
        [SerializeField, Min(0.3f)] private float _maxDistanceFromViewer = 1.4f;

        private Bounds _placedFor;
        private bool _placed;

        private void Awake()
        {
            Bind(_rotate, OnRotate);
            Bind(_reposition, OnReposition);
            Bind(_delete, OnDelete);
            Bind(_close, OnClose);
        }

        private void Start()
        {
            if (_controller == null) return;
            _controller.ModeChanged += OnModeChanged;
            OnModeChanged(_controller.Mode);
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.ModeChanged -= OnModeChanged;
        }

        private void LateUpdate()
        {
            // Follow the piece when it turns in place or another piece is selected.
            if (_panel == null || !_panel.activeSelf || _controller == null) return;
            if (_controller.TryGetSelectedBounds(out var bounds) && (!_placed || bounds != _placedFor)) PlaceBeside(bounds);
        }

        private void OnModeChanged(BuildMode mode)
        {
            if (_panel == null) return;

            bool show = mode == BuildMode.Modify;
            _placed = false;
            _panel.SetActive(show);
            if (show && _controller.TryGetSelectedBounds(out var bounds)) PlaceBeside(bounds);
        }

        private void PlaceBeside(Bounds piece)
        {
            _placedFor = piece;
            _placed = true;
            if (_viewer == null) return;

            var toViewer = Vector3.ProjectOnPlane(_viewer.position - piece.center, Vector3.up);
            toViewer = toViewer.sqrMagnitude > 0.0001f ? toViewer.normalized : -Vector3.ProjectOnPlane(_viewer.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, -toViewer);

            // Out from the near side, across to the right-hand edge, a third of the way up.
            var e = piece.extents;
            float depth = Mathf.Abs(toViewer.x) * e.x + Mathf.Abs(toViewer.z) * e.z;
            float width = Mathf.Abs(right.x) * e.x + Mathf.Abs(right.z) * e.z;

            var position = piece.center + toViewer * (depth + _standOff) + right * width;
            position.y = Mathf.Clamp(piece.min.y + piece.size.y * 0.33f, _viewer.position.y - 0.9f, _viewer.position.y - 0.1f);

            var fromViewer = position - _viewer.position;
            if (fromViewer.magnitude > _maxDistanceFromViewer) position = _viewer.position + fromViewer.normalized * _maxDistanceFromViewer;

            // Face the eyes squarely, tilting up or down as needed, so the text reads flat.
            var facing = position - _viewer.position;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing.sqrMagnitude > 0.0001f ? facing : -toViewer, Vector3.up));
        }

        private void OnRotate()
        {
            if (_controller != null) _controller.RotateSelected();
        }

        private void OnReposition()
        {
            if (_controller != null) _controller.RepositionSelected();
        }

        private void OnDelete()
        {
            if (_controller != null) _controller.DeleteSelected();
        }

        private void OnClose()
        {
            if (_controller != null) _controller.CloseSelection();
        }

        private static void Bind(Button button, UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
