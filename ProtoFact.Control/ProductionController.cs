using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Domain;
using ProtoFact.Engine;

namespace ProtoFact.Control
{
    /// <summary>
    /// Drives production based on defined goals.
    /// </summary>
    public class ProductionController : IProductionController
    {
        private readonly IRateSolver _rateSolver;
        private readonly IInventory _inventory;
        private readonly ILogger _logger;

        private readonly List<IProcessor> _processors = new();
        private readonly List<ProductionGoal> _goals = new();
        private readonly Dictionary<Item, double> _lastAppliedGoalRates = new();

        public IList<IProcessor> Processors => _processors;

        public ProductionController(
            IRateSolver rateSolver,
            IInventory inventory,
            ILogger logger)
        {
            _rateSolver = rateSolver ?? throw new ArgumentNullException(nameof(rateSolver));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void AddGoal(IProductionGoal goal)
        {
            if (goal is not ProductionGoal pg)
                throw new ArgumentException("Invalid goal type.");

            _goals.Add(pg);
        }

        public void Tick(IEnumerable<Recipe> recipes)
        {
            foreach (var goal in _goals)
            {
                if (_lastAppliedGoalRates.TryGetValue(goal.Target, out var lastRate) &&
                    Math.Abs(lastRate - goal.TargetRate) < 1e-6)
                {
                    // No change in goal → skip recomputing plan
                    continue;
                }

                var plan = _rateSolver.SolveRate(
                                                 goal.Target,
                                                 goal.TargetRate,
                                                 recipes);

                ApplyPlan(plan, recipes);

                _lastAppliedGoalRates[goal.Target] = goal.TargetRate;
            }
        }

        private void ApplyPlan(
            ProductionNode node,
            IEnumerable<Recipe> recipes)
        {
            if (node.MachinesRequired <= 0)
                return;

            var currentCount = CountProcessors(node.Item);
            var requiredCount = (int)Math.Ceiling(node.MachinesRequired);

            if (requiredCount > currentCount)
            {
                var recipe = recipes.FirstOrDefault(r => r.Output.Item.Equals(node.Item));
                if (recipe == null)
                    return;

                var toCreate = requiredCount - currentCount;

                for (int i = 0; i < toCreate; i++)
                {
                    _processors.Add(CreateProcessor(recipe));
                }

                _logger.Debug($"Scaled UP '{node.Item.Name}' to {requiredCount} processors.");
            }
            else if (requiredCount < currentCount)
            {
                var toRemove = currentCount - requiredCount;

                RemoveProcessors(node.Item, toRemove);

                _logger.Debug($"Scaled DOWN '{node.Item.Name}' to {requiredCount} processors.");
            }

            // ✅ ADD THIS BLOCK BACK (this is what you're missing)

            if (node.Inputs == null || node.Inputs.Count == 0)
                return;

            foreach (var input in node.Inputs)
            {
                ApplyPlan(input, recipes);
            }
        }

        private void RemoveProcessors(Item item, int count)
        {
            for (int i = _processors.Count - 1; i >= 0 && count > 0; i--)
            {
                var p = _processors[i];

                if (p.Recipe.Output.Item.Equals(item) &&
                    p.State != ProcessorState.Running)
                {
                    _processors.RemoveAt(i);
                    count--;
                }
            }
        }

        private int CountProcessors(Item item)
        {
            return _processors.Count(p => p.Recipe.Output.Item.Equals(item));
        }

        private IProcessor CreateProcessor(Recipe recipe)
        {
            return new Processor(recipe, _inventory, _logger);
        }
    }
}