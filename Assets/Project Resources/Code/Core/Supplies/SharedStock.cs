using System;
using System.Collections.Generic;

namespace CampfireFriends.Supplies
{
    /// <summary>
    /// R02 — the session's shared construction stock. Spending is all-or-nothing so a
    /// multi-resource cost can never leave the pool partially charged or negative.
    /// Only the authority (host) should mutate this; clients mirror it.
    /// </summary>
    public class SharedStock : IStockView
    {
        private readonly Dictionary<ResourceType, int> _amounts = new Dictionary<ResourceType, int>();

        /// <summary>Raised after any change with the resource and its new total.</summary>
        public event Action<ResourceType, int> StockChanged;

        public int Get(ResourceType type) => _amounts.TryGetValue(type, out var amount) ? amount : 0;

        public void Add(ResourceType type, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to remove stock.");
            if (amount == 0) return;

            _amounts[type] = Get(type) + amount;
            StockChanged?.Invoke(type, _amounts[type]);
        }

        public void Add(IEnumerable<ResourceAmount> amounts)
        {
            foreach (var a in amounts) Add(a.Type, a.Amount);
        }

        public bool CanAfford(IReadOnlyList<ResourceAmount> cost) => StockViewExtensions.CanAfford(this, cost);

        /// <summary>Spends the whole cost or nothing.</summary>
        public bool TrySpend(IReadOnlyList<ResourceAmount> cost)
        {
            if (!CanAfford(cost)) return false;

            foreach (var total in StockMath.Totals(cost))
            {
                if (total.Value == 0) continue;
                _amounts[total.Key] = Get(total.Key) - total.Value;
                StockChanged?.Invoke(total.Key, _amounts[total.Key]);
            }
            return true;
        }
    }
}
