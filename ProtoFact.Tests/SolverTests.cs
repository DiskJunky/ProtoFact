using System.Collections.Generic;
using ProtoFact.Domain;
using ProtoFact.Engine;
using Xunit;

namespace ProtoFact.Tests
{
    public class SolverTests
    {
        private Item Create(string id) => new(id, id, ItemType.Intermediate);

        [Fact]
        public void Solve_Should_Return_Base_Resource_When_No_Recipe()
        {
            var ore = Create("ore");

            var solver = new Solver();

            var result = solver.Solve(ore, 10, new List<Recipe>());

            Assert.Single(result);
            Assert.Equal(10, result[ore]);
        }

        [Fact]
        public void Solve_Should_Expand_Single_Level()
        {
            var ore = Create("ore");
            var plate = Create("plate");

            var recipe = new Recipe(
                new[] { new Quantity(ore, 2) },
                new Quantity(plate, 1),
                1);

            var solver = new Solver();

            var result = solver.Solve(plate, 3, new[] { recipe });

            Assert.Equal(6, result[ore]);
        }

        [Fact]
        public void Solve_Should_Expand_Multi_Level()
        {
            var ore = Create("ore");
            var plate = Create("plate");
            var gear = Create("gear");

            var plateRecipe = new Recipe(
                new[] { new Quantity(ore, 2) },
                new Quantity(plate, 1),
                1);

            var gearRecipe = new Recipe(
                new[] { new Quantity(plate, 2) },
                new Quantity(gear, 1),
                1);

            var solver = new Solver();

            var result = solver.Solve(gear, 2, new[] { plateRecipe, gearRecipe });

            // 2 gear → 4 plate → 8 ore
            Assert.Equal(8, result[ore]);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(5, 10)]
        [InlineData(10, 20)]
        public void Solve_Should_Scale_Linearly(double gearQty, double expectedOre)
        {
            var ore = Create("ore");
            var plate = Create("plate");

            var recipe = new Recipe(
                new[] { new Quantity(ore, 2) },
                new Quantity(plate, 1),
                1);

            var solver = new Solver();

            var result = solver.Solve(plate, gearQty, new[] { recipe });

            Assert.Equal(expectedOre, result[ore]);
        }
    }
}