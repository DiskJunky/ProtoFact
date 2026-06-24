using System.Collections.Generic;
using System.Linq;

namespace ProtoFact.Abstractions
{
    public class ValidationResult
    {
        private readonly List<string> _errors = new();

        public bool IsValid => !_errors.Any();
        public IReadOnlyList<string> Errors => _errors;

        public void AddError(string error)
        {
            _errors.Add(error);
        }
    }
}