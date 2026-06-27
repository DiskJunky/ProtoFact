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
        private readonly IAdaptiveController _adaptiveController;
        private readonly IBufferStrategy _bufferStrategy;

        public IList<IProcessor> Processors => _processors;

        public ProductionController(
            IRateSolver rateSolver,
            IAdaptiveController adaptiveController,
            IBufferStrategy bufferStrategy,
            IInventory inventory,
            ILogger logger)
        {
            _rateSolver = rateSolver ?? throw new ArgumentNullException(nameof(rateSolver));
            _adaptiveController = adaptiveController ?? throw new ArgumentNullException(nameof(adaptiveController));
            _bufferStrategy = bufferStrategy ?? throw new ArgumentNullException(nameof(bufferStrategy));
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
            var visited = new HashSet<Item>();

            foreach (var goal in _goals)
            {
                var plan = _rateSolver.SolveRate(
                                                 goal.Target,
                                                 goal.TargetRate,
                                                 recipes);
                
                ApplyPlanInternal(plan, recipes, visited);
            }
        }

        private void ApplyPlan(
            ProductionNode node,
            IEnumerable<Recipe> recipes)
        {
            var visited = new HashSet<Item>();
            ApplyPlanInternal(node, recipes, visited);
        }

        private void ApplyPlanInternal(
            ProductionNode node,
            IEnumerable<Recipe> recipes,
            HashSet<Item> visited)
        {
            // ✅ Prevent double-processing in DAG
            if (!visited.Add(node.Item))
                return;

            if (node.MachinesRequired <= 0)
                return;

            // ✅ Process inputs FIRST
            if (node.Inputs != null && node.Inputs.Count > 0)
            {
                foreach (var input in node.Inputs)
                {
                    ApplyPlanInternal(input, recipes, visited);
                }
            }

            // ✅ EXISTING SCALING LOGIC (UNCHANGED BELOW)
            var recipe = recipes.FirstOrDefault(r => r.Output.Item.Equals(node.Item));
            if (recipe == null)
                return;

            var currentStock = _inventory.Get(node.Item);
            var currentCount = CountProcessors(node.Item);
            var requiredCount = (int)Math.Ceiling(node.MachinesRequired);

            var currentThroughput = GetThroughput(node.Item);
            var requiredThroughput = node.RequiredRate;

            // small tolerance for float noise
            var flowSatisfied = currentThroughput >= requiredThroughput * 0.98;


            var targetBuffer = _bufferStrategy.GetTargetBuffer(node.Item, node.RequiredRate);

            var deficit = targetBuffer - currentStock;
            var deadband = targetBuffer * 0.1; // 10%

            if (Math.Abs(deficit) < deadband)
            {
                deficit = 0;
            }
            var hasDeficit = deficit > 0;

            // ✅ HARD FLOOR: never go below required processors
            var minDelta = requiredCount - currentCount;
            var baseDelta = minDelta;

            var cappedStock = Math.Min(currentStock, targetBuffer * 2);

            var adaptiveDelta = _adaptiveController.ComputeAdjustment(
                node.Item,
                cappedStock,
                targetBuffer);

            // ✅ dampen response
            adaptiveDelta *= 0.25;   // try 0.2–0.4 range

            // ✅ NEW: stock cannot prevent scale-down once flow is satisfied
            if (flowSatisfied)
            {
                adaptiveDelta = Math.Min(0, adaptiveDelta);
            }

            var adaptive = baseDelta + adaptiveDelta;

            // ✅ enforce minimum required processors
            var adjustment = Math.Max(minDelta, adaptive);

            // ✅ Prevent unnecessary growth above required capacity
            if (adjustment > 0 && currentCount >= requiredCount)
            {
                if (hasDeficit)
                {
                    // Allow controlled overscaling to recover buffer
                    var recoveryTimeSeconds = 3.0; // tuning knob

                    var extraNeeded = (int)Math.Ceiling(
                                                        deficit / (node.RequiredRate * recoveryTimeSeconds)
                                                       ); var maxAllowed = requiredCount + extraNeeded;

                    var allowedIncrease = maxAllowed - currentCount;
                    adjustment = Math.Min(adjustment, allowedIncrease);
                }
                else
                {
                    // No deficit → no scaling above required
                    adjustment = 0;
                }
            }

            const int maxStepPerTick = 2;
            adjustment = Math.Clamp(adjustment, -maxStepPerTick, maxStepPerTick);

            if (adjustment > 0 && currentCount >= requiredCount)
            {
                adjustment = Math.Min(adjustment, 1);
            }

            if (adjustment > 0 && !CanSustainProduction(recipe))
            {
                _logger.Debug($"Cannot scale '{node.Item.Name}' due to insufficient inputs.");
                return;
            }

            if (adjustment < 0 && currentCount + adjustment < requiredCount)
            {
                // ❌ Prevent dropping below required production
                return;
            }

            // ✅ NEW: do not scale down if flow is already insufficient
            if (adjustment < 0 && !flowSatisfied)
            {
                adjustment = 0;
            }

            _logger.Debug(
                          $"[Adaptive] {node.Item.Name} | " +
                          $"Stock={currentStock:F2} Target={targetBuffer:F2} " +
                          $"Req={requiredCount} Curr={currentCount} MinΔ={minDelta:F2} " +
                          $"Flow={(flowSatisfied ? "OK" : "LOW")} " +
                          $"BaseΔ={baseDelta:F2} AdaptΔ={adaptiveDelta:F2} FinalΔ={adjustment:F2}"
                         );

            // ✅ Quantise adjustment to discrete actuator steps
            if (Math.Abs(adjustment) < 1)
            {
                adjustment = 0;
            }
            else
            {
                adjustment = (int)Math.Round(adjustment);
            }
            ApplyAdjustment(node.Item, adjustment, recipe);
        }

        private void ApplyAdjustment(Item item, double adjustment, Recipe recipe)
        {
            const double discreteDeadband = 0.1;

            if (Math.Abs(adjustment) < discreteDeadband)
                return;

            var currentCount = CountProcessors(item);

            if (adjustment > 0)
            {
                var toAdd = (int)Math.Floor(adjustment);

                for (int i = 0; i < toAdd; i++)
                {
                    _processors.Add(CreateProcessor(recipe));
                }

                _logger.Debug($"Scaled UP '{item.Name}' by {toAdd} → {currentCount + toAdd}");
            }
            else
            {
                var toRemove = (int)Math.Floor(Math.Abs(adjustment));

                RemoveProcessors(item, toRemove);

                _logger.Debug($"Scaled DOWN '{item.Name}' by {toRemove} → {Math.Max(0, currentCount - toRemove)}");
            }
        }

        private bool IsSourceRecipe(Recipe recipe)
        {
            return recipe.Inputs == null || recipe.Inputs.Count == 0;
        }

        public IEnumerable<BottleneckInfo> GetBottleneckInfo()
        {
            foreach (var group in _processors.GroupBy(p => p.Recipe.Output.Item))
            {
                var total = group.Count();
                var running = group.Count(p => p.IsRunning);

                if (total == 0)
                    continue;

                var utilization = (double)running / total;
                var severity = 1.0 - utilization;

                yield return new BottleneckInfo(
                                                group.Key,
                                                total,
                                                running,
                                                utilization,
                                                severity
                                               );
            }
        }

        public IEnumerable<BottleneckInfo> GetTopBottlenecks(int topN = 3)
        {
            return GetBottleneckInfo()
                   .Where(b => b.Severity > 0)
                   .OrderByDescending(b => b.Severity)
                   .ThenByDescending(b => b.TotalProcessors)
                   .Take(topN);
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
            if (count <= 0)
                return;

            // ✅ Only mark RUNNING processors
            var candidates = _processors
                             .Where(p => p.Recipe.Output.Item.Equals(item))
                             .Where(p => p.IsRunning)                      // ✅ CRITICAL
                             .Where(p => !p.IsMarkedForRemoval)
                             .Take(count)
                             .ToList();

            foreach (var p in candidates)
            {
                p.IsMarkedForRemoval = true;
            }

            // ✅ Clean up stopped processors
            _processors.RemoveAll(p =>
                                      p.Recipe.Output.Item.Equals(item) &&
                                      p.IsMarkedForRemoval &&
                                      p.State == ProcessorState.Stopped);
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

        public SystemMetrics GetSystemMetrics()
        {
            var total = _processors.Count;
            if (total == 0)
                return new SystemMetrics(0, 0, 0, 1, 0);

            var running = _processors.Count(p => p.IsRunning);

            var utilization = (double)running / total;
            var idle = 1.0 - utilization;

            return new SystemMetrics(
                                     total,
                                     running,
                                     utilization,
                                     idle,
                                     utilization
                                    );
        }

        public double GetThroughput(Item item)
        {
            var processors = _processors
                             .Where(p => p.Recipe.Output.Item.Equals(item))
                             .ToList();

            if (processors.Count == 0)
                return 0;

            double total = 0;

            foreach (var p in processors)
            {
                if (!p.IsRunning)
                    continue;

                var ratePerProcessor = p.Recipe.Output.Amount / p.Recipe.DurationSeconds;
                total += ratePerProcessor;
            }

            return total;
        }

        public double GetMaxThroughput(Item item, IEnumerable<Recipe> recipes)
        {
            var recipeMap = recipes.ToDictionary(r => r.Output.Item);

            return ComputeMaxThroughput(item, recipeMap);
        }

        private double ComputeMaxThroughput(
            Item item,
            Dictionary<Item, Recipe> recipeMap)
        {
            // ✅ If no processors produce this item → zero capacity
            var processors = _processors
                             .Where(p => p.Recipe.Output.Item.Equals(item))
                             .ToList();

            if (processors.Count == 0)
                return 0;

            // ✅ Local capacity (sum of all processors)
            double localCapacity = processors.Sum(p =>
                                                      p.Recipe.Output.Amount / p.Recipe.DurationSeconds);

            // ✅ If no recipe → raw source
            if (!recipeMap.TryGetValue(item, out var recipe) ||
                recipe.Inputs.Count == 0)
            {
                return localCapacity;
            }

            // ✅ Compute upstream constraints
            double upstreamLimit = double.MaxValue;

            foreach (var input in recipe.Inputs)
            {
                var inputCapacity = ComputeMaxThroughput(input.Item, recipeMap);

                // Required input per output unit
                double ratio = input.Amount / recipe.Output.Amount;

                if (ratio <= 0)
                    continue;

                // Limit imposed by this input
                double constraint = inputCapacity / ratio;

                upstreamLimit = Math.Min(upstreamLimit, constraint);
            }

            // ✅ Final limit = lowest constraint
            return Math.Min(localCapacity, upstreamLimit);
        }

        public IEnumerable<IProductionGoal> GetGoals()
        {
            return _goals;
        }

    }
}