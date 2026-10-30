using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// One tooltip shared by a menu's buttons, on the Interaction SDK Tooltip prefab. It shows a
    /// title and a line of detail above whichever button asks, its down arrow pointing at the button.
    /// It never takes pointer input, so it can't steal the hover it was opened for. The prefab's
    /// boxes are a fixed size, so it widens to the longest line and grows by any extra lines.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TooltipView : MonoBehaviour
    {
        [SerializeField] private string _titleName = "Title";
        [SerializeField] private string _subtitleName = "Subtitle";
        [SerializeField] private string _arrowName = "TooltipArrowDown";
        [SerializeField] private string _hiddenArrowName = "TooltipArrowUp";

        [Tooltip("Canvas units between the button's top edge and the tooltip.")]
        [SerializeField, Min(0f)] private float _gap = 12f;

        private RectTransform _root;
        private TMP_Text _title;
        private TMP_Text _subtitle;
        private RectTransform _arrow;
        private Object _owner;

        // The subtitle and each box up to the root, with their saved sizes, to grow from on each show.
        private RectTransform[] _boxes = new RectTransform[0];
        private Vector2[] _savedSizes = new Vector2[0];

        private void Awake() => Init();

        /// <summary>Runs on first use as well, since the tooltip is saved hidden and hidden objects don't wake.</summary>
        private void Init()
        {
            if (_root != null) return;
            _root = (RectTransform)transform;
            var texts = GetComponentsInChildren<TMP_Text>(true);
            _title = texts.FirstOrDefault(t => t.name == _titleName);
            _subtitle = texts.FirstOrDefault(t => t.name == _subtitleName);
            _arrow = FindChild(_arrowName) as RectTransform;

            var hidden = FindChild(_hiddenArrowName);
            if (hidden != null) hidden.gameObject.SetActive(false);

            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // It sits on the menu's canvas; keep any layout group there from arranging it.
            var layout = GetComponent<LayoutElement>();
            if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var boxes = new System.Collections.Generic.List<RectTransform>();
            for (var box = _subtitle != null ? _subtitle.rectTransform : null; box != null; box = box.parent as RectTransform)
            {
                boxes.Add(box);
                if (box == _root) break;
            }
            _boxes = boxes.ToArray();
            _savedSizes = _boxes.Select(box => box.sizeDelta).ToArray();
        }

        /// <summary>Shows the tooltip above <paramref name="anchor"/>; <paramref name="owner"/> is who may hide it again.</summary>
        public void Show(Object owner, RectTransform anchor, string title, string detail)
        {
            Init();
            _owner = owner;

            if (_title != null) _title.text = title;
            if (_subtitle != null)
            {
                _subtitle.text = detail;
                _subtitle.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            }
            if (_arrow != null) _arrow.gameObject.SetActive(true);

            gameObject.SetActive(true);
            _root.SetAsLastSibling();
            FitText(title, detail);
            PlaceAbove(anchor);
        }

        /// <summary>Hides the tooltip if <paramref name="owner"/> is the one showing it.</summary>
        public void Hide(Object owner)
        {
            if (owner != _owner) return;
            _owner = null;
            gameObject.SetActive(false);
        }

        /// <summary>Back to the saved size, then wider for the longest line and taller for each extra subtitle line.</summary>
        private void FitText(string title, string detail)
        {
            for (int i = 0; i < _boxes.Length; i++) _boxes[i].sizeDelta = _savedSizes[i];
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            if (_subtitle == null || _boxes.Length == 0) return;

            var subtitleSize = _subtitle.GetPreferredValues(detail ?? "");
            float longest = Mathf.Max(subtitleSize.x, _title != null ? _title.GetPreferredValues(title ?? "").x : 0f);
            float wider = Mathf.Max(0f, longest - _subtitle.rectTransform.rect.width + 4f);
            float taller = Mathf.Max(0f, subtitleSize.y - _savedSizes[0].y);
            if (wider <= 0f && taller <= 0f) return;

            // Layout groups here set widths from the root down but leave heights alone, so the
            // root takes the extra width and every box from the subtitle up takes the extra height.
            for (int i = 0; i < _boxes.Length; i++)
                _boxes[i].sizeDelta = _savedSizes[i] + new Vector2(_boxes[i] == _root ? wider : 0f, taller);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        /// <summary>Moves the tooltip, in its parent's plane, so its bottom sits just above the anchor and its arrow lines up with the anchor's centre.</summary>
        private void PlaceAbove(RectTransform anchor)
        {
            var parent = _root.parent;
            if (parent == null || anchor == null) return;

            var anchorRect = anchor.rect;
            var target = parent.InverseTransformPoint(anchor.TransformPoint(new Vector3(anchorRect.center.x, anchorRect.yMax)));

            var rootRect = _root.rect;
            var bottom = parent.InverseTransformPoint(_root.TransformPoint(new Vector3(rootRect.center.x, rootRect.yMin)));
            float pointerX = _arrow != null ? parent.InverseTransformPoint(_arrow.position).x : bottom.x;

            _root.localPosition += new Vector3(target.x - pointerX, target.y + _gap - bottom.y, 0f);
        }

        private Transform FindChild(string childName)
        {
            return GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != transform && t.name == childName);
        }
    }
}
