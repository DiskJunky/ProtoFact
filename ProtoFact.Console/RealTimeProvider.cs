using System;
using ProtoFact.Abstractions;

namespace ProtoFact.Console
{
    /// <summary>
    /// Provides real-world time deltas for simulation.
    /// </summary>
    public sealed class RealTimeProvider : ITimeProvider
    {
        private DateTime _last = DateTime.UtcNow;

        public double DeltaTime
        {
            get
            {
                var now = DateTime.UtcNow;
                var delta = (now - _last).TotalSeconds;
                _last = now;
                return delta;
            }
        }
    }
}