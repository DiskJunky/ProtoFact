using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    public interface IInventory
    {
        bool CanConsume(IEnumerable<Quantity> required);
        bool TryConsume(IEnumerable<Quantity> required);
        void Add(IEnumerable<Quantity> quantities);
        double Get(Item item);
    }
}
