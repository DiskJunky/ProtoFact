using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Builds display-oriented production trees for a set of goals. Uses the
    /// same rate math as <see cref="RateSolver"/>, but - unlike
    /// <see cref="RateSolver"/> - intentionally creates a separate node
    /// instance for every edge (even for the same item), so a UI can render
    /// the full chain from each goal down to raw materials. Shared items
    /// (required by more than one edge across the whole plan) are flagged
    /// via <see cref="ProductionTreeNode.IsShared"/>.
    /// </summary>
    public class ProductionTreeBuilder : IProductionTreeBuilder
    {
        public IReadOnlyList<ProductionTreeNode> BuildTrees(
            IEnumerable<IProductionGoal> goals,
            IEnumerable<Recipe> recipes,
            IInventorySnapshot inventory)
        {
            if (goals == null) throw new ArgumentNullException(nameof(goals));
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));

            var goalList = goals.ToList();
            var recipeMap = recipes.ToDictionary(r => r.Output.Item);

            // First pass: accumulate total demand and count how many
            // distinct edges require each item, across all goals.
            var totalRate = new Dictionary<Item, double>();
            var edgeCount = new Dictionary<Item, int>();

            foreach (var goal in goalList)
            {
                Accumulate(goal.Target, goal.TargetRate, recipeMap, totalRate, edgeCount);
            }

            // Second pass: build one (duplicated) tree per goal using the
            // totals gathered above.
            var trees = new List<ProductionTreeNode>();
            foreach (var goal in goalList)
            {
                trees.Add(BuildNode(goal.Target, goal.TargetRate, recipeMap, totalRate, edgeCount, inventory));
            }

            return trees;
        }

        private static void Accumulate(
            Item item,
            double rate,
            Dictionary<Item, Recipe> recipeMap,
            Dictionary<Item, double> totalRate,
            Dictionary<Item, int> edgeCount)
        {
            totalRate.TryGetValue(item, out var existing);
            totalRate[item] = existing + rate;

            if (!recipeMap.TryGetValue(item, out var recipe))
                return;

            var outputRate = recipe.Output.Amount / recipe.DurationSeconds;
            var scale = rate / outputRate;

            foreach (var input in recipe.Inputs)
            {
                var inputRate = scale * (input.Amount / recipe.DurationSeconds);

                edgeCount.TryGetValue(input.Item, out var count);
                edgeCount[input.Item] = count + 1;

                Accumulate(input.Item, inputRate, recipeMap, totalRate, edgeCount);
            }
        }

        private static ProductionTreeNode BuildNode(
            Item item,
            double edgeRate,
            Dictionary<Item, Recipe> recipeMap,
            Dictionary<Item, double> totalRate,
            Dictionary<Item, int> edgeCount,
            IInventorySnapshot inventory)
        {
            var node = new ProductionTreeNode
            {
                Item = item,
                EdgeRate = edgeRate,
                AggregateRate = totalRate.TryGetValue(item, out var total) ? total : edgeRate,
                IsShared = edgeCount.TryGetValue(item, out var count) && count > 1,
                CurrentStock = inventory.Get(item),
            };

            if (!recipeMap.TryGetValue(item, out var recipe))
                return node;

            var outputRate = recipe.Output.Amount / recipe.DurationSeconds;
            node.MachinesRequired = edgeRate / outputRate;

            var scale = edgeRate / outputRate;

            foreach (var input in recipe.Inputs)
            {
                var inputRate = scale * (input.Amount / recipe.DurationSeconds);

                node.Inputs.Add(BuildNode(input.Item, inputRate, recipeMap, totalRate, edgeCount, inventory));
            }

            return node;
        }
    }
}
