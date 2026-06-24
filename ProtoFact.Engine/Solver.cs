using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Expands recipe dependencies to compute required base resources.
    /// </summary>
    public class Solver : ISolver
    {
        public IDictionary<Item, double> Solve(
            Item target,
            double quantity,
            IEnumerable<Recipe> recipes)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            var recipeMap = recipes.ToDictionary(r => r.Output.Item);

            var result = new Dictionary<Item, double>();

            Expand(target, quantity, recipeMap, result);

            return result;
        }

        private void Expand(
            Item item,
            double requiredAmount,
            Dictionary<Item, Recipe> recipeMap,
            Dictionary<Item, double> result)
        {
            // Base resource (no recipe produces it)
            if (!recipeMap.TryGetValue(item, out var recipe))
            {
                if (!result.ContainsKey(item))
                    result[item] = 0;

                result[item] += requiredAmount;
                result[item] = MathUtil.Normalize(result[item]);
                return;
            }

            // Scale recipe based on required output
            var scale = requiredAmount / recipe.Output.Amount;

            foreach (var input in recipe.Inputs)
            {
                Expand(
                       input.Item,
                       input.Amount * scale,
                       recipeMap,
                       result);
            }
        }
    }
}