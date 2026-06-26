using ProtoFact.Domain;

namespace ProtoFact.Abstractions;

public interface IBufferStrategy
{
    double GetTargetBuffer(Item item, double rate);
}
