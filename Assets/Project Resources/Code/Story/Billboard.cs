using UnityEngine;

namespace CampfireFriends.Story
{
    /// <summary>
    /// Keeps a small label upright and turned toward the viewer, and stood a little out from where
    /// it was placed on their side, so a label on a wall-mounted board isn't half inside the wall.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("How far (metres) the label stands out toward the viewer.")]
        [SerializeField, Min(0f)] private float _towardViewer = 0.15f;

        private Transform _viewer;
        private Vector3 _anchor;

        private void Awake() => _anchor = transform.position;

        private void LateUpdate()
        {
            if (_viewer == null && Camera.main != null) _viewer = Camera.main.transform;
            if (_viewer == null) return;
            var away = Vector3.ProjectOnPlane(_anchor - _viewer.position, Vector3.up);
            if (away.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            transform.position = _anchor - away.normalized * Mathf.Min(_towardViewer, away.magnitude * 0.5f);
        }
    }
}
