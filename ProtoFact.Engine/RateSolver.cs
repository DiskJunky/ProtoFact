using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Calculates production rates and required processors.
    /// </summary>
    public class RateSolver : IRateSolver
    {
        public ProductionNode SolveRate(
            Item target,
            double ratePerSecond,
            IEnumerable<Recipe> recipes)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (ratePerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(ratePerSecond));

            var recipeMap = recipes.ToDictionary(r => r.Output.Item);

            // ✅ Shared node map (this creates the DAG)
            var nodeMap = new Dictionary<Item, ProductionNode>();

            return BuildNode(target, ratePerSecond, recipeMap, nodeMap);
        }

        private ProductionNode BuildNode(
            Item item,
            double requiredRate,
            Dictionary<Item, Recipe> recipeMap,
            Dictionary<Item, ProductionNode> nodeMap)
        {
            if (!nodeMap.TryGetValue(item, out var node))
            {
                node = new ProductionNode
                       {
                           Item = item,
                           RequiredRate = 0,
                           MachinesRequired = 0,
                           Inputs = new List<ProductionNode>()
                       };

                nodeMap[item] = node;
            }

            // ✅ Accumulate demand
            node.RequiredRate += requiredRate;

            // Base resource (no recipe)
            if (!recipeMap.TryGetValue(item, out var recipe))
            {
                node.MachinesRequired = 0;
                return node;
            }

            var outputRate = recipe.Output.Amount / recipe.DurationSeconds;

            // ✅ Accumulate machines (not overwrite!)
            node.MachinesRequired += requiredRate / outputRate;

            // ✅ Build inputs (shared references)
            foreach (var input in recipe.Inputs)
            {
                var inputRate = requiredRate * (input.Amount / recipe.Output.Amount);

                var child = BuildNode(
                                      input.Item,
                                      inputRate,
                                      recipeMap,
                                      nodeMap);

                // ✅ Avoid duplicates
                if (!node.Inputs.Contains(child))
                {
                    node.Inputs.Add(child);
                }
            }

            return node;
        }
    }
}
