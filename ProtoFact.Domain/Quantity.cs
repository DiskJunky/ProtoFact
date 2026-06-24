using System;

namespace ProtoFact.Domain
{
    /// <summary>
    /// Represents a quantity of a specific item.
    /// </summary>
    public sealed class Quantity
    {
        public Item Item { get; }
        public double Amount { get; }

        public Quantity(Item item, double amount)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0");

            Amount = amount;
        }

        public override string ToString()
        {
            return $"{Amount} x {Item.Name}";
        }
    }
}