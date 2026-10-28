using System.Collections.Generic;
using CampfireFriends.Supplies;
using NUnit.Framework;

namespace CampfireFriends.Tests
{
    /// <summary>Acceptance checks from design doc §5.</summary>
    public class SuppliesTests
    {
        private static List<ResourceAmount> Cost(params ResourceAmount[] amounts) => new List<ResourceAmount>(amounts);

        [Test]
        public void TrySpend_IsAllOrNothing_AcrossResourceTypes()
        {
            var stock = new SharedStock();
            stock.Add(ResourceType.Boards, 5);
            stock.Add(ResourceType.Scrap, 1);

            bool spent = stock.TrySpend(Cost(new ResourceAmount(ResourceType.Boards, 3), new ResourceAmount(ResourceType.Scrap, 2)));

            Assert.IsFalse(spent);
            Assert.AreEqual(5, stock.Get(ResourceType.Boards), "Boards must not be charged when scrap is short.");
            Assert.AreEqual(1, stock.Get(ResourceType.Scrap));
        }

        [Test]
        public void TrySpend_NeverGoesNegative()
        {
            var stock = new SharedStock();
            stock.Add(ResourceType.Boards, 2);

            Assert.IsTrue(stock.TrySpend(Cost(new ResourceAmount(ResourceType.Boards, 2))));
            Assert.IsFalse(stock.TrySpend(Cost(new ResourceAmount(ResourceType.Boards, 1))));
            Assert.AreEqual(0, stock.Get(ResourceType.Boards));
        }

        [Test]
        public void CanAfford_SumsDuplicateEntries()
        {
            var stock = new SharedStock();
            stock.Add(ResourceType.Boards, 3);

            var cost = Cost(new ResourceAmount(ResourceType.Boards, 2), new ResourceAmount(ResourceType.Boards, 2));

            Assert.IsFalse(stock.CanAfford(cost));
        }

        [Test]
        public void SupplyNode_TwoWithdrawals_NeverExceedRemaining()
        {
            var stock = new SharedStock();
            var shed = new SupplyNode("shed_lumber", ResourceType.Boards, 4);

            int first = shed.Collect(3, stock);
            int second = shed.Collect(3, stock);

            Assert.AreEqual(3, first);
            Assert.AreEqual(1, second);
            Assert.AreEqual(4, stock.Get(ResourceType.Boards), "No duplicate supplies from one node.");
            Assert.IsTrue(shed.IsDepleted);
            Assert.AreEqual(0, shed.Collect(1, stock));
        }

        [Test]
        public void SupplyNode_RaisesDepletedOnce()
        {
            var stock = new SharedStock();
            var crate = new SupplyNode("crate_scrap", ResourceType.Scrap, 2);
            int depletedCount = 0;
            crate.Depleted += _ => depletedCount++;

            crate.Collect(2, stock);
            crate.Collect(2, stock);

            Assert.AreEqual(1, depletedCount);
        }
    }
}
