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
    /// Buttons beyond the catalog are hidden.
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

        private UnityAction[] _handlers = new UnityAction[0];

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
