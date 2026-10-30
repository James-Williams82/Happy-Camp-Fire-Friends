using System.Linq;
using CampfireFriends.Building;
using CampfireFriends.Game;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// A world-space piece palette: button i places catalog piece i. It labels its buttons from the
    /// catalog and forwards clicks to the build controller as commands; it keeps no build state.
    /// Buttons beyond the catalog are hidden. The panel opens at the viewer's side in build mode while
    /// nothing is in hand, and gets out of the way once a piece is picked or selected.
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
        [Tooltip("The visible panel, shown in build mode while nothing is in hand. A child, so this component keeps listening while it's hidden.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("The player's head and how far away the menu sits.")]
        [SerializeField] private ViewerPlacement _placement = new ViewerPlacement();

        [Tooltip("Where at the player's side the menu sits.")]
        [SerializeField] private MenuDock _dock = new MenuDock();

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

        private void LateUpdate() => _dock.Tick(transform, _placement, Time.deltaTime);

        /// <summary>
        /// Open at the player's side only while choosing (nothing in hand). Picking a piece puts it
        /// away so the build is clear to place on; a selected piece has its own modify menu. Back (B)
        /// brings the palette back to pick another piece.
        /// </summary>
        private void OnModeChanged(BuildMode mode)
        {
            if (_panel == null) return;

            bool show = mode == BuildMode.Browse;
            if (show && !_panel.activeSelf) _dock.Dock(transform, _placement);
            else if (!show) _dock.Stop(transform);
            _panel.SetActive(show);
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

                var tooltip = slot.GetComponent<ButtonTooltip>();
                if (tooltip != null) tooltip.SetText(definition.DisplayName, DetailText(definition));

                int index = i;
                _handlers[i] = () => Select(index);
                slot.onClick.AddListener(_handlers[i]);
            }

            ShowCosts();
            GameModes.Changed += ShowCosts;
        }

        /// <summary>Each piece's cost, or "Free" when the game mode makes building free.</summary>
        private void ShowCosts()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var definition = _catalog != null ? _catalog.Get(i) : null;
                if (_slots[i] == null || definition == null) continue;
                SetText(_slots[i], _subtitleName, GameModes.Rules.FreeBuilding ? "Free" : CostText(definition));
            }
        }

        private void OnDisable()
        {
            GameModes.Changed -= ShowCosts;
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

        /// <summary>Size in metres and what the piece needs to stand on, e.g. "1 × 1 × 1 m · stacks on ground or pieces".</summary>
        private string DetailText(PieceDefinition definition)
        {
            float cell = _catalog.CellSize;
            var size = definition.Footprint;
            var detail = $"{size.x * cell:0.#} × {size.y * cell:0.#} × {size.z * cell:0.#} m · {SupportText(definition.Support)}";
            if (!string.IsNullOrEmpty(definition.Hint)) detail += $"\n{definition.Hint}";
            return definition.Use == PieceUse.Toggle ? $"{detail}\nOutside build mode, point and pull the trigger to switch it." : detail;
        }

        private static string SupportText(SupportRule rule)
        {
            switch (rule)
            {
                case SupportRule.Stack: return "stacks on ground or pieces";
                case SupportRule.Ground: return "must stand on the ground";
                case SupportRule.Roof: return "rests on pieces or joins a roof";
                case SupportRule.SideAttach: return "attaches to any side";
                default: return "goes anywhere";
            }
        }

        private static void SetText(Button slot, string childName, string text)
        {
            var label = slot.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == childName);
            if (label != null) label.text = text;
        }
    }
}
