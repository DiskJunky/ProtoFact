using System.Collections.Generic;
using ProtoFact.Control;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Tests.Fakes;
using Xunit;

namespace ProtoFact.Tests
{
    public class ProductionControllerTests
    {
        private Item Create(string id) => new(id, id, ItemType.Intermediate);

        [Fact]
        public void Should_Create_Processors_For_Goal()
        {
            var ore = Create("ore");
            var plate = Create("plate");

            var recipe = new Recipe(
                                    new[] { new Quantity(ore, 1) },
                                    new Quantity(plate, 1),
                                    1);

            var controller = new ProductionController(
                                                      new RateSolver(),
                                                      new Inventory(),
                                                      new FakeLogger());

            controller.AddGoal(new ProductionGoal(plate, 2));

            controller.Tick(new[] { recipe });

            Assert.True(controller.Processors.Count >= 2);
        }

        [Fact]
        public void Should_Not_Create_Processors_When_No_Recipe()
        {
            var ore = Create("ore");

            var controller = new ProductionController(
                                                      new RateSolver(),
                                                      new Inventory(),
                                                      new FakeLogger());

            controller.AddGoal(new ProductionGoal(ore, 5));

            controller.Tick(new List<Recipe>());

            Assert.Empty(controller.Processors);
        }

        [Fact]
        public void Should_Not_Duplicate_Processors_On_Repeated_Ticks()
        {
            var ore = Create("ore");
            var plate = Create("plate");

            var recipe = new Recipe(
                                    new[] { new Quantity(ore, 1) },
                                    new Quantity(plate, 1),
                                    1);

            var controller = new ProductionController(
                                                      new RateSolver(),
                                                      new Inventory(),
                                                      new FakeLogger());

            controller.AddGoal(new ProductionGoal(plate, 1));

            controller.Tick(new[] { recipe });
            var firstCount = controller.Processors.Count;

            controller.Tick(new[] { recipe });

            Assert.Equal(firstCount, controller.Processors.Count);
        }
    }
}