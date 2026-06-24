using ProtoFact.Domain;
using ProtoFact.Engine;
using Xunit;

namespace ProtoFact.Tests
{
    public class RateSolverTests
    {
        private Item Create(string id) => new(id, id, ItemType.Intermediate);

        [Fact]
        public void Should_Calculate_Single_Level_Rate()
        {
            var ore = Create("ore");
            var plate = Create("plate");

            var recipe = new Recipe(
                new[] { new Quantity(ore, 2) },
                new Quantity(plate, 1),
                durationSeconds: 1);

            var solver = new RateSolver();

            var result = solver.SolveRate(plate, 2, new[] { recipe });

            Assert.Equal(2, result.MachinesRequired);
            Assert.Single(result.Inputs);
            Assert.Equal(4, result.Inputs[0].RequiredRate);
        }

        [Fact]
        public void Should_Calculate_Multi_Level_Rate()
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

            var solver = new RateSolver();

            var result = solver.SolveRate(gear, 1, new[] { plateRecipe, gearRecipe });

            // Gear needs 2 plate/sec → 4 ore/sec
            Assert.Equal(1, result.MachinesRequired);
            Assert.Equal(2, result.Inputs[0].RequiredRate);
            Assert.Equal(4, result.Inputs[0].Inputs[0].RequiredRate);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void Machines_Should_Scale_Linearly(double rate)
        {
            var ore = Create("ore");
            var plate = Create("plate");

            var recipe = new Recipe(
                new[] { new Quantity(ore, 1) },
                new Quantity(plate, 1),
                1);

            var solver = new RateSolver();

            var result = solver.SolveRate(plate, rate, new[] { recipe });

            Assert.Equal(rate, result.MachinesRequired);
        }
    }
}
