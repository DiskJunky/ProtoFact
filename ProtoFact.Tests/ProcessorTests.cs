using ProtoFact.Abstractions;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Tests.Fakes;
using Xunit;

namespace ProtoFact.Tests
{
    public class ProcessorTests
    {
        private readonly Item _ore = new("ore", "Ore", ItemType.Raw);
        private readonly Item _plate = new("plate", "Plate", ItemType.Intermediate);

        private Recipe CreateRecipe()
        {
            return new Recipe(
                new[] { new Quantity(_ore, 2) },
                new Quantity(_plate, 1),
                durationSeconds: 2
            );
        }

        [Fact]
        public void Should_Not_Start_Without_Resources()
        {
            var inv = new Inventory();
            var logger = new FakeLogger();
            var proc = new Processor(CreateRecipe(), inv, logger);


            proc.Tick(1);

            Assert.Equal(ProcessorState.Idle, proc.State);
        }

        [Fact]
        public void Should_Start_When_Resources_Available()
        {
            var inv = new Inventory();
            inv.Add(new[] { new Quantity(_ore, 2) });

            var logger = new FakeLogger();
            var proc = new Processor(CreateRecipe(), inv, logger);

            proc.Tick(0.1);

            Assert.Equal(ProcessorState.Running, proc.State);
        }

        [Fact]
        public void Should_Consume_Inputs_On_Start()
        {
            var inv = new Inventory();
            inv.Add(new[] { new Quantity(_ore, 2) });

            var logger = new FakeLogger();
            var proc = new Processor(CreateRecipe(), inv, logger);

            proc.Tick(0.1);

            Assert.Equal(0, inv.Get(_ore));
        }

        [Fact]
        public void Should_Progress_Over_Time()
        {
            var inv = new Inventory();
            inv.Add(new[] { new Quantity(_ore, 2) });

            var logger = new FakeLogger();
            var proc = new Processor(CreateRecipe(), inv, logger);

            proc.Tick(0.1); // start
            proc.Tick(1.0);

            Assert.True(proc.Progress > 0);
        }

        [Fact]
        public void Should_Complete_And_Output()
        {
            var inv = new Inventory();
            inv.Add(new[] { new Quantity(_ore, 2) });

            var logger = new FakeLogger();
            var proc = new Processor(CreateRecipe(), inv, logger);

            proc.Tick(0.1); // start
            proc.Tick(2.0); // finish

            Assert.Equal(1, inv.Get(_plate));
            Assert.Equal(ProcessorState.Idle, proc.State);
        }

        [Fact]
        public void Should_Not_Partially_Complete()
        {
            var inv = new Inventory();
            inv.Add(new[] { new Quantity(_ore, 2) });

            var logger = new FakeLogger();
            var proc = new Processor(CreateRecipe(), inv, logger);

            proc.Tick(0.1); // start
            proc.Tick(0.5);

            Assert.Equal(0, inv.Get(_plate));
        }
    }
}