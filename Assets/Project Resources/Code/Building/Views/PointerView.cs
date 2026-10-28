using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>The build pointer ray from the aim source to whatever it hits.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PointerView : MonoBehaviour
    {
        [Tooltip("Metres to start the visible ray in front of the origin, so it isn't drawn into the camera.")]
        [SerializeField, Min(0f)] private float _startOffset = 0.05f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.enabled = false;
        }

        public void Show(Ray ray, Vector3 end)
        {
            _line.SetPosition(0, ray.origin + ray.direction * _startOffset);
            _line.SetPosition(1, end);
            _line.enabled = true;
        }

        public void Hide()
        {
            if (_line != null) _line.enabled = false;
        }
    }
}
