using ProtoFact.Abstractions;
using ProtoFact.Domain;
using System;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Executes a recipe over time using inventory resources.
    /// </summary>
    public class Processor : IProcessor
    {
        private readonly IInventory _inventory;
        private readonly ILogger _logger;


        public Recipe Recipe { get; }

        public ProcessorState State { get; private set; }

        public double Progress { get; private set; }

        public bool IsRunning => State == ProcessorState.Running;

        public string Name { get; }

        public Processor(Recipe recipe, IInventory inventory, ILogger logger)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            State = ProcessorState.Idle;
            Progress = 0;
        }

        public void Tick(double deltaTime)
        {
            if (deltaTime <= 0)
                return;

            switch (State)
            {
                case ProcessorState.Idle:
                    TryStart();
                    break;

                case ProcessorState.Running:
                    Run(deltaTime);
                    break;
            }
        }

        private void TryStart()
        {
            _logger.Trace($"Processor starting recipe: {Recipe}");
            if (_inventory.TryConsume(Recipe.Inputs))
            {
                Progress = 0;
                State = ProcessorState.Running;
            }
        }

        private void Run(double deltaTime)
        {
            Progress += deltaTime / Recipe.DurationSeconds;
            Progress = Math.Min(Progress, 1.0);

            if (MathUtil.GreaterOrEqual(Progress, 1.0))
            {
                Complete();
            }
        }

        private void Complete()
        {
            _logger.Info($"Completed recipe: {Recipe.Output}");
            _inventory.Add(new[] { Recipe.Output });

            Progress = 0;
            State = ProcessorState.Idle;
        }
    }
}