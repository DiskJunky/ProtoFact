using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    public class ModelValidator : IModelValidator
    {
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

                if (recipe.Inputs == null || !recipe.Inputs.Any())
                    result.AddError($"Recipe '{recipe}' has no inputs.");

                if (recipe.DurationSeconds <= 0)
                    result.AddError($"Recipe '{recipe}' has invalid duration.");
            }
        }

        private void ValidateCycles(List<Recipe> recipes, ValidationResult result)
        {
            var graph = BuildDependencyGraph(recipes);

            var visited = new HashSet<Item>();
            var stack = new HashSet<Item>();

            foreach (var node in graph.Keys)
            {
                if (DetectCycle(node, graph, visited, stack))
                {
                    result.AddError($"Cycle detected involving item '{node.Id}'");
                }
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