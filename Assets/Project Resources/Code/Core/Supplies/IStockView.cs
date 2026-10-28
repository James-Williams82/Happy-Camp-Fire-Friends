using System;
using System.Collections.Generic;

namespace CampfireFriends.Supplies
{
    /// <summary>
    /// Read-only access to a stock pool. The host reads its authoritative <see cref="SharedStock"/>;
    /// clients read a replicated mirror. Previews only ever need this side.
    /// </summary>
    public interface IStockView
    {
        int Get(ResourceType type);
    }

    public static class StockViewExtensions
    {
        public static bool CanAfford(this IStockView stock, IReadOnlyList<ResourceAmount> cost)
        {
            // Sum per type first so a cost listing the same resource twice is still checked correctly.
            foreach (var total in StockMath.Totals(cost))
            {
                if (stock.Get(total.Key) < total.Value) return false;
            }
            return true;
        }
    }

    internal static class StockMath
    {
        public static Dictionary<ResourceType, int> Totals(IReadOnlyList<ResourceAmount> cost)
        {
            var totals = new Dictionary<ResourceType, int>();
            if (cost == null) return totals;

            foreach (var a in cost)
            {
                if (a.Amount < 0) throw new ArgumentException($"Negative cost entry: {a}");
                totals[a.Type] = (totals.TryGetValue(a.Type, out var t) ? t : 0) + a.Amount;
            }
            return totals;
        }
    }
}
