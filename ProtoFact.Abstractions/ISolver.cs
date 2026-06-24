using System.Collections.Generic;
using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface ISolver
    {
        IDictionary<Item, double> Solve(
            Item target,
            double quantity,
            IEnumerable<Recipe> recipes);
    }
}