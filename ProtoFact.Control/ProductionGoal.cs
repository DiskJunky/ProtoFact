using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Control
{
    /// <summary>
    /// Represents a desired production target (items per second).
    /// </summary>
    public class ProductionGoal : IProductionGoal
    {
        public Item Target { get; }
        public double TargetRate { get; }

        public ProductionGoal(Item target, double targetRate)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));

            if (targetRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetRate));

            TargetRate = targetRate;
        }
    }
}