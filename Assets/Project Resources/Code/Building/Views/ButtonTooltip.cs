using UnityEngine;
using UnityEngine.EventSystems;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// Shows its text on the menu's shared <see cref="TooltipView"/> while a ray or finger hovers
    /// this button. The text is set in the inspector, or by the menu that owns the button.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TooltipView _view;
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _detail;

        public void SetText(string title, string detail)
        {
            _title = title;
            _detail = detail;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_view != null && !string.IsNullOrEmpty(_title)) _view.Show(this, (RectTransform)transform, _title, _detail);
        }

        public void OnPointerExit(PointerEventData eventData) => Hide();

        private void OnDisable() => Hide();

        private void Hide()
        {
            if (_view != null) _view.Hide(this);
        }
    }
}
