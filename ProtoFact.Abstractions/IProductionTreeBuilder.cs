using System.Collections.Generic;
using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    /// <summary>
    /// Builds display-oriented recipe/production trees (one per goal) for
    /// UI consumption. See <see cref="ProductionTreeNode"/> for how shared
    /// dependencies are represented.
    /// </summary>
    public interface IProductionTreeBuilder
    {
        IReadOnlyList<ProductionTreeNode> BuildTrees(
            IEnumerable<IProductionGoal> goals,
            IEnumerable<Recipe> recipes,
            IInventorySnapshot inventory);
    }

    /// <summary>
    /// Minimal read-only view over inventory needed to annotate a
    /// production tree with current stock levels.
    /// </summary>
    public interface IInventorySnapshot
    {
        double Get(Item item);
    }
}
