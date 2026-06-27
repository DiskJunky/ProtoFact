using ProtoFact.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoFact.Domain
{
    /// <summary>
    /// Represents a transformation from input items to a single output item.
    /// </summary>
    public sealed class Recipe
    {
        public IReadOnlyList<Quantity> Inputs { get; }

        public Quantity Output { get; }

        public double DurationSeconds { get; }

        public Recipe(IEnumerable<Quantity> inputs, Quantity output, double durationSeconds)
        {
            Inputs = inputs?.ToList() ?? throw new ArgumentNullException(nameof(inputs));

            Output = output ?? throw new ArgumentNullException(nameof(output));

            if (durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be > 0");

            DurationSeconds = durationSeconds;
        }

        public override string ToString()
        {
            var inputStr = string.Join(", ", Inputs.Select(x => x.ToString()));
            return $"{inputStr} -> {Output}";
        }
    }
}