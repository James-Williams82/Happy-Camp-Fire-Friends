using System.Collections.Generic;
using System.Text;
using CampfireFriends.Game;
using CampfireFriends.Story;
using CampfireFriends.Supplies;
using TMPro;
using UnityEngine;

namespace CampfireFriends.Construction
{
    /// <summary>
    /// The inventory line on the build menu: the crew's building supplies, and the Night 1 items
    /// in hand (the fuse until it's in the panel, the fuel until it's poured, flashlight, walkie).
    /// </summary>
    public class InventoryReadout : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField, Min(0.05f)] private float _refresh = 0.25f;

        private static readonly (string name, Night01Flag taken, Night01Flag usedUp)[] Carried =
        {
            ("Walkie", Night01Flag.RadioCheckIn, Night01Flag.None),
            ("Flashlight", Night01Flag.FlashlightCollected, Night01Flag.None),
            ("Fuse", Night01Flag.FuseCollected, Night01Flag.FuseInserted),
            ("Fuel can", Night01Flag.FuelCollected, Night01Flag.GeneratorFueled),
        };

        private float _next;

        private void OnEnable() => _next = 0f;

        private void Update()
        {
            if (_text == null || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + _refresh;
            _text.text = Describe();
        }

        private static string Describe()
        {
            var sb = new StringBuilder("<b>INVENTORY</b>   ");
            var backend = ConstructionBackends.Current;
            if (GameModes.Rules.FreeBuilding) sb.Append("Building is free");
            else if (backend?.Stock == null) sb.Append("No supplies");
            else
            {
                var stock = backend.Stock;
                var parts = new List<string>();
                foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType))) parts.Add($"{type} {stock.Get(type)}");
                sb.Append(string.Join("  ·  ", parts));
            }

            var story = Stories.Current;
            if (story != null && Stories.Running)
            {
                var items = new List<string>();
                foreach (var (name, taken, usedUp) in Carried)
                    if (story.State.Has(taken) && (usedUp == Night01Flag.None || !story.State.Has(usedUp))) items.Add(name);
                sb.Append("\n<size=80%>Carrying: ").Append(items.Count > 0 ? string.Join(", ", items) : "nothing").Append("</size>");
            }
            return sb.ToString();
        }
    }
}
