using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface IProductionGoal
    {
        Item Target { get; }
        double TargetRate { get; }
    }
}
