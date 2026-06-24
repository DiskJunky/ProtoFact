using System;
using System.Collections.Generic;
using ProtoFact.Abstractions;

namespace ProtoFact.Engine
{
    /// <summary>
    /// Coordinates processors and advances the simulation.
    /// </summary>
    public class Engine : IEngine
    {
        private readonly IEnumerable<IProcessor> _processors;
        private readonly ITimeProvider _timeProvider;

        public Engine(IEnumerable<IProcessor> processors, ITimeProvider timeProvider)
        {
            _processors = processors ?? throw new ArgumentNullException(nameof(processors));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public void Tick()
        {
            var deltaTime = _timeProvider.DeltaTime;

            if (deltaTime <= 0)
                return;

            foreach (var processor in _processors)
            {
                processor.Tick(deltaTime);
            }
        }
    }
}
