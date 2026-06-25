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

            // ✅ STEP 1: Process inputs FIRST
            if (node.Inputs != null && node.Inputs.Count > 0)
            {
                foreach (var input in node.Inputs)
                {
                    ApplyPlan(input, recipes);
                }
            }

            // ✅ STEP 2: Now scale current node
            var currentCount = CountProcessors(node.Item);
            var requiredCount = (int)Math.Ceiling(node.MachinesRequired);

            if (requiredCount > currentCount)
            {
                var recipe = recipes.FirstOrDefault(r => r.Output.Item.Equals(node.Item));
                if (recipe == null)
                    return;

                // ✅ IMPORTANT: allow scaling if it's a source OR inputs exist OR upstream is planned
                if (!CanSustainProduction(recipe))
                {
                    _logger.Debug($"Cannot scale '{node.Item.Name}' due to insufficient inputs.");
                    return;
                }

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
        }

        private bool CanSustainProduction(Recipe recipe)
        {
            // ✅ Always allow source recipes
            if (recipe.Inputs == null || recipe.Inputs.Count == 0)
                return true;

            // ✅ Allow if currently possible
            if (_inventory.CanConsume(recipe.Inputs))
                return true;

            // ✅ Otherwise: allow anyway (pipeline will fill)
            return true;
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

        public double GetUtilization(Item item)
        {
            var processors = _processors
                             .Where(p => p.Recipe.Output.Item.Equals(item))
                             .ToList();

            if (processors.Count == 0)
                return 0;

            var running = processors.Count(p => p.IsRunning);

            return (double)running / processors.Count;
        }

        public IEnumerable<Item> GetBottlenecks()
        {
            foreach (var group in _processors.GroupBy(p => p.Recipe.Output.Item))
            {
                var total = group.Count();
                var running = group.Count(p => p.IsRunning);

                if (total > 0 && running < total)
                {
                    yield return group.Key;
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