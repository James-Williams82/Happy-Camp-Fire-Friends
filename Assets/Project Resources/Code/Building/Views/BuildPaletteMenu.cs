using System.Linq;
using CampfireFriends.Building;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// A world-space piece palette: button i places catalog piece i. It labels its buttons from the
    /// catalog and forwards clicks to the build controller as commands; it keeps no build state.
    /// Buttons beyond the catalog are hidden. The panel shows only in build mode, and each time it
    /// appears it is set in front of the viewer.
    /// </summary>
    public class BuildPaletteMenu : MonoBehaviour
    {
        [SerializeField] private BuildController _controller;
        [SerializeField] private PieceCatalog _catalog;

        [Tooltip("One button per catalog piece, in catalog order.")]
        [SerializeField] private Button[] _slots = new Button[0];

        [Tooltip("Child text objects the piece name and cost are written to.")]
        [SerializeField] private string _labelName = "Label";
        [SerializeField] private string _subtitleName = "Subtitle";

        [Header("Presentation")]
        [Tooltip("The visible panel, shown only in build mode. A child, so this component keeps listening while it's hidden.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("The player's head. When set, the menu appears in front of it on entering build mode.")]
        [SerializeField] private Transform _viewer;

        [SerializeField, Min(0.2f)] private float _distance = 0.6f;
        [SerializeField] private float _heightBelowEyes = 0.25f;

        private UnityAction[] _handlers = new UnityAction[0];

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

        private void OnModeChanged(BuildMode mode)
        {
            if (_panel == null) return;

            bool show = mode != BuildMode.None;
            if (show && !_panel.activeSelf) PlaceInFrontOfViewer();
            _panel.SetActive(show);
        }

        private void PlaceInFrontOfViewer()
        {
            if (_viewer == null) return;

            var forward = Vector3.ProjectOnPlane(_viewer.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.ProjectOnPlane(_viewer.up, Vector3.up);
            forward.Normalize();

            transform.position = _viewer.position + forward * _distance + Vector3.down * _heightBelowEyes;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void OnEnable()
        {
            _handlers = new UnityAction[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;

                var definition = _catalog != null ? _catalog.Get(i) : null;
                slot.gameObject.SetActive(definition != null);
                if (definition == null) continue;

                SetText(slot, _labelName, definition.DisplayName);
                SetText(slot, _subtitleName, CostText(definition));

                int index = i;
                _handlers[i] = () => Select(index);
                slot.onClick.AddListener(_handlers[i]);
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _handlers.Length && i < _slots.Length; i++)
            {
                if (_slots[i] != null && _handlers[i] != null) _slots[i].onClick.RemoveListener(_handlers[i]);
            }
            _handlers = new UnityAction[0];
        }

        private void Select(int index)
        {
            if (_controller != null) _controller.SelectPiece(index);
        }

        private static string CostText(PieceDefinition definition)
        {
            return string.Join("  ", definition.Cost.Select(cost => $"{cost.Amount} {cost.Type}"));
        }

        private static void SetText(Button slot, string childName, string text)
        {
            var label = slot.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == childName);
            if (label != null) label.text = text;
        }
    }
}
