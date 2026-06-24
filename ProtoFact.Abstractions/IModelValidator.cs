using ProtoFact.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProtoFact.Abstractions
{
    public interface IModelValidator
    {
        ValidationResult Validate(IEnumerable<Recipe> recipes);
    }
}