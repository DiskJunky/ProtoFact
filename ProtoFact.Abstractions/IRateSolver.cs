using System.Collections.Generic;
using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface IRateSolver
    {
        ProductionNode SolveRate(
            Item target,
            double ratePerSecond,
            IEnumerable<Recipe> recipes);
    }
}