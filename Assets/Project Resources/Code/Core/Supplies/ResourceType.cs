using System;

namespace CampfireFriends.Supplies
{
    /// <summary>Materials guards can recover and spend on repairs (design doc §5).</summary>
    public enum ResourceType
    {
        Boards = 0,
        Scrap = 1,
        Batteries = 2,
    }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Type;
        public int Amount;

        public ResourceAmount(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public override string ToString() => $"{Amount} {Type}";
    }
}
