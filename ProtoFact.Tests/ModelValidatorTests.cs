using System.Collections.Generic;
using ProtoFact.Domain;
using ProtoFact.Engine;
using Xunit;

namespace ProtoFact.Tests
{
    public class ModelValidatorTests
    {
        [Fact]
        public void Should_Detect_Cycle()
        {
            var a = new Item("a", "A", ItemType.Intermediate);
            var b = new Item("b", "B", ItemType.Intermediate);

            var recipes = new List<Recipe>
                          {
                              new Recipe(new[]{ new Quantity(a,1)}, new Quantity(b,1), 1),
                              new Recipe(new[]{ new Quantity(b,1)}, new Quantity(a,1), 1),
                          };

            var validator = new ModelValidator();

            var result = validator.Validate(recipes);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Should_Pass_Valid_DAG()
        {
            var ore = new Item("ore", "Ore", ItemType.Raw);
            var plate = new Item("plate", "Plate", ItemType.Intermediate);

            var recipes = new List<Recipe>
                          {
                              new Recipe(new[]{ new Quantity(ore,1)}, new Quantity(plate,1), 1)
                          };

            var validator = new ModelValidator();

            var result = validator.Validate(recipes);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Recipe_Should_Throw_When_Invalid_Duration(double duration)
        {
            var item = new Item("x", "X", ItemType.Intermediate);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new Recipe(new[] { new Quantity(item, 1) },
                           new Quantity(item, 1),
                           duration);
            });
        }
    }
}