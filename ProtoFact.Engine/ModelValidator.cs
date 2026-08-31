using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Validates recipe definitions, ensuring structural correctness and absence of cycles.
    /// </summary>
    public class ModelValidator : IModelValidator
    {
        /// <summary>
        /// Validates the provided recipes.
        /// </summary>
        public ValidationResult Validate(IEnumerable<Recipe> recipes)
        {
            var result = new ValidationResult();

            var recipeList = recipes.ToList();

            ValidateBasic(recipeList, result);
            ValidateCycles(recipeList, result);

            return result;
        }

        private void ValidateBasic(List<Recipe> recipes, ValidationResult result)
        {
            foreach (var recipe in recipes)
            {
                if (recipe.Output == null)
                    result.AddError("Recipe has null output.");

                // Raw resources may be produced by "generator" recipes with
                // no inputs (e.g. mining ore). Any other recipe must consume
                // at least one input.
                var isRawGenerator = recipe.Output?.Item.Type == ItemType.Raw;

                if (!isRawGenerator && (recipe.Inputs == null || !recipe.Inputs.Any()))
                    result.AddError($"Recipe '{recipe}' has no inputs.");
            }
        }

        private void ValidateCycles(List<Recipe> recipes, ValidationResult result)
        {
            var graph = BuildDependencyGraph(recipes);

            var visited = new HashSet<Item>();
            var stack = new HashSet<Item>();

            bool cycleDetected = false;

            foreach (var node in graph.Keys)
            {
                if (DetectCycle(node, graph, visited, stack))
                {
                    cycleDetected = true;
                    break;
                }
            }

            if (cycleDetected)
            {
                result.AddError("Cycle detected in recipe graph.");
            }
        }

        private Dictionary<Item, List<Item>> BuildDependencyGraph(List<Recipe> recipes)
        {
            var graph = new Dictionary<Item, List<Item>>();

            foreach (var recipe in recipes)
            {
                var output = recipe.Output.Item;

                if (!graph.ContainsKey(output))
                    graph[output] = new List<Item>();

                foreach (var input in recipe.Inputs)
                {
                    graph[output].Add(input.Item);

                    if (!graph.ContainsKey(input.Item))
                        graph[input.Item] = new List<Item>();
                }
            }

            return graph;
        }

        private bool DetectCycle(
            Item node,
            Dictionary<Item, List<Item>> graph,
            HashSet<Item> visited,
            HashSet<Item> stack)
        {
            if (stack.Contains(node))
                return true;

            if (visited.Contains(node))
                return false;

            visited.Add(node);
            stack.Add(node);

            foreach (var neighbor in graph[node])
            {
                if (DetectCycle(neighbor, graph, visited, stack))
                    return true;
            }

            stack.Remove(node);
            return false;
        }
    }
}