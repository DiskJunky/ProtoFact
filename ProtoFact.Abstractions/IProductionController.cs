using System.Collections.Generic;
using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface IProductionController
    {
        IList<IProcessor> Processors { get; }

        void AddGoal(IProductionGoal goal);

        void Tick(IEnumerable<Recipe> recipes);
    }
}