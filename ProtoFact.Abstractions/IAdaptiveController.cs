using ProtoFact.Domain;

namespace ProtoFact.Abstractions;

public interface IAdaptiveController
{
    double ComputeAdjustment(Item item, double currentStock, double targetStock);
}
