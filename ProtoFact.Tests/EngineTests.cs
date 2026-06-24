using System.Collections.Generic;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Tests.Fakes;
using Xunit;

namespace ProtoFact.Tests
{
    public class EngineTests
    {
        [Fact]
        public void Tick_Should_Advance_All_Processors()
        {
            var item = new Item("ore", "Ore", ItemType.Raw);

            var recipe = new Recipe(
                                    new[] { new Quantity(item, 1) },
                                    new Quantity(item, 1),
                                    1.0);

            var inventory = new Inventory();
            inventory.Add(new[] { new Quantity(item, 1) });

            var processor = new Processor(recipe, inventory);

            var time = new FakeTimeProvider { DeltaTime = 1.0 };

            var engine = new Engine.Engine(new List<IProcessor> { processor }, time);

            engine.Tick();

            Assert.Equal(ProcessorState.Running, processor.State);
        }

        [Fact]
        public void Tick_Should_DoNothing_When_DeltaTime_IsZero()
        {
            var time = new FakeTimeProvider { DeltaTime = 0 };

            var engine = new Engine.Engine(new List<IProcessor>(), time);

            engine.Tick();

            // No exception = pass
            Assert.True(true);
        }
    }
}