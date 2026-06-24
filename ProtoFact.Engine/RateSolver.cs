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

            return BuildNode(target, ratePerSecond, recipeMap);
        }

        private ProductionNode BuildNode(
            Item item,
            double requiredRate,
            Dictionary<Item, Recipe> recipeMap)
        {
            var node = new ProductionNode
                       {
                           Item = item,
                           RequiredRate = requiredRate
                       };

            // Base resource
            if (!recipeMap.TryGetValue(item, out var recipe))
            {
                node.MachinesRequired = 0;
                return node;
            }

            // Output rate per machine
            var outputRate = recipe.Output.Amount / recipe.DurationSeconds;

            // Machines needed
            node.MachinesRequired = requiredRate / outputRate;

            // Inputs
            foreach (var input in recipe.Inputs)
            {
                var inputRate = requiredRate * (input.Amount / recipe.Output.Amount);

                node.Inputs.Add(BuildNode(
                                          input.Item,
                                          inputRate,
                                          recipeMap));
            }

            return node;
        }
    }
}
