using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>Draws the edges of a world box with a <see cref="LineRenderer"/>; used to show a piece's logical footprint.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BoundsOutlineView : MonoBehaviour
    {
        [SerializeField] private Material _validMaterial;
        [SerializeField] private Material _invalidMaterial;

        private LineRenderer _line;
        private readonly Vector3[] _points = new Vector3[16];

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = _points.Length;
            _line.enabled = false;
        }

        public void Show(Bounds bounds, bool valid)
        {
            var a = bounds.min;
            var b = bounds.max;

            // One continuous path over all 12 edges (some edges are traced twice).
            _points[0] = new Vector3(a.x, a.y, a.z);
            _points[1] = new Vector3(b.x, a.y, a.z);
            _points[2] = new Vector3(b.x, a.y, b.z);
            _points[3] = new Vector3(a.x, a.y, b.z);
            _points[4] = new Vector3(a.x, a.y, a.z);
            _points[5] = new Vector3(a.x, b.y, a.z);
            _points[6] = new Vector3(b.x, b.y, a.z);
            _points[7] = new Vector3(b.x, a.y, a.z);
            _points[8] = new Vector3(b.x, b.y, a.z);
            _points[9] = new Vector3(b.x, b.y, b.z);
            _points[10] = new Vector3(b.x, a.y, b.z);
            _points[11] = new Vector3(b.x, b.y, b.z);
            _points[12] = new Vector3(a.x, b.y, b.z);
            _points[13] = new Vector3(a.x, a.y, b.z);
            _points[14] = new Vector3(a.x, b.y, b.z);
            _points[15] = new Vector3(a.x, b.y, a.z);

            _line.SetPositions(_points);
            var material = valid ? _validMaterial : _invalidMaterial;
            if (material != null) _line.sharedMaterial = material;
            _line.enabled = true;
        }

        public void Hide()
        {
            if (_line != null) _line.enabled = false;
        }
    }
}
