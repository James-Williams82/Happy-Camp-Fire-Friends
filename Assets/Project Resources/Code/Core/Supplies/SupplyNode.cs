using System;

namespace CampfireFriends.Supplies
{
    /// <summary>
    /// R01 — a finite source of authorized supplies (maintenance shed, lumber stack, supply crate).
    /// Withdrawals are clamped to what's left, so two simultaneous requests can never take more than exists.
    /// </summary>
    public class SupplyNode
    {
        public string Id { get; }
        public ResourceType Type { get; }
        public int Remaining { get; private set; }
        public bool IsDepleted => Remaining <= 0;

        public event Action<SupplyNode, int> Collected;
        public event Action<SupplyNode> Depleted;

        public SupplyNode(string id, ResourceType type, int quantity)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Supply nodes need a durable id.", nameof(id));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));

            Id = id;
            Type = type;
            Remaining = quantity;
        }

        /// <summary>Takes up to <paramref name="requested"/> units into <paramref name="stock"/>; returns how many were taken.</summary>
        public int Collect(int requested, SharedStock stock)
        {
            if (requested <= 0 || IsDepleted) return 0;

            int taken = Math.Min(requested, Remaining);
            Remaining -= taken;
            stock.Add(Type, taken);

            Collected?.Invoke(this, taken);
            if (IsDepleted) Depleted?.Invoke(this);
            return taken;
        }
    }
}
