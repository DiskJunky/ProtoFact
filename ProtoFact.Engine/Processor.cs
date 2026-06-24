using System;
using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Executes a recipe over time using inventory resources.
    /// </summary>
    public class Processor : IProcessor
    {
        private readonly IInventory _inventory;

        public Recipe Recipe { get; }
        public ProcessorState State { get; private set; }
        public double Progress { get; private set; }

        public Processor(Recipe recipe, IInventory inventory)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

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
            if (_inventory.TryConsume(Recipe.Inputs))
            {
                Progress = 0;
                State = ProcessorState.Running;
            }
        }

        private void Run(double deltaTime)
        {
            Progress += deltaTime / Recipe.DurationSeconds;

            if (MathUtil.GreaterOrEqual(Progress, 1.0))
            {
                Complete();
            }
        }

        private void Complete()
        {
            _inventory.Add(new[] { Recipe.Output });

            Progress = 0;
            State = ProcessorState.Idle;
        }
    }
}