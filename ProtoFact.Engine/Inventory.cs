using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Deterministic inventory. Not thread-safe. Owned by engine.
    /// </summary>
    public class Inventory : IInventory
    {
        private readonly Dictionary<Item, double> _stock = new();

        public double Get(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return _stock.TryGetValue(item, out var value) ? value : 0;
        }

        public void Add(IEnumerable<Quantity> quantities)
        {
            foreach (var qty in quantities)
            {
                if (!_stock.ContainsKey(qty.Item))
                    _stock[qty.Item] = 0;

                _stock[qty.Item] += qty.Amount;
                _stock[qty.Item] = MathUtil.Normalize(_stock[qty.Item]);
            }
        }

        public bool CanConsume(IEnumerable<Quantity> required)
        {
            var grouped = NormalizeInputs(required);

            foreach (var qty in grouped)
            {
                var available = Get(qty.Item);

                if (MathUtil.LessThan(available, qty.Amount))
                    return false;
            }

            return true;
        }

        public bool TryConsume(IEnumerable<Quantity> required)
        {
            var grouped = NormalizeInputs(required);

            if (!CanConsume(grouped))
                return false;

            foreach (var qty in grouped)
            {
                _stock[qty.Item] -= qty.Amount;
                _stock[qty.Item] = MathUtil.Normalize(_stock[qty.Item]);
            }

            return true;
        }

        private static IEnumerable<Quantity> NormalizeInputs(IEnumerable<Quantity> required)
        {
            return required
                   .GroupBy(q => q.Item)
                   .Select(g => new Quantity(g.Key, g.Sum(x => x.Amount)));
        }
    }
}
