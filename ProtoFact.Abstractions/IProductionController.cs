using System.Collections.Generic;
using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface IProductionController
    {
        IList<IProcessor> Processors { get; }

        void AddGoal(IProductionGoal goal);

        IEnumerable<Item> GetBottlenecks();

        double GetUtilization(Item item);

        void Tick(IEnumerable<Recipe> recipes);
    }
}