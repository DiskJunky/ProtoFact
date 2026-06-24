using System.Collections.Generic;
using ProtoFact.Domain;
using ProtoFact.Engine;
using Xunit;

namespace ProtoFact.Tests
{
    public class InventoryTests
    {
        private readonly Item _iron = new("iron", "Iron", ItemType.Raw);
        private readonly Item _plate = new("plate", "Plate", ItemType.Intermediate);

        [Fact]
        public void Add_ShouldIncreaseStock()
        {
            var inv = new Inventory();

            inv.Add(new[] { new Quantity(_iron, 10) });

            Assert.Equal(10, inv.Get(_iron));
        }

        [Fact]
        public void CanConsume_ShouldReturnTrue_WhenEnoughStock()
        {
            var inv = new Inventory();

            inv.Add(new[] { new Quantity(_iron, 10) });

            var result = inv.CanConsume(new[] { new Quantity(_iron, 5) });

            Assert.True(result);
        }

        [Fact]
        public void CanConsume_ShouldReturnFalse_WhenInsufficientStock()
        {
            var inv = new Inventory();

            inv.Add(new[] { new Quantity(_iron, 3) });

            var result = inv.CanConsume(new[] { new Quantity(_iron, 5) });

            Assert.False(result);
        }

        [Fact]
        public void TryConsume_ShouldBeAtomic()
        {
            var inv = new Inventory();

            inv.Add(new[]
            {
                new Quantity(_iron, 5),
                new Quantity(_plate, 1)
            });

            var result = inv.TryConsume(new[]
            {
                new Quantity(_iron, 5),
                new Quantity(_plate, 2)
            });

            Assert.False(result);

            Assert.Equal(5, inv.Get(_iron));
            Assert.Equal(1, inv.Get(_plate));
        }

        [Fact]
        public void TryConsume_ShouldDeduct_WhenValid()
        {
            var inv = new Inventory();

            inv.Add(new[] { new Quantity(_iron, 10) });

            var success = inv.TryConsume(new[] { new Quantity(_iron, 4) });

            Assert.True(success);
            Assert.Equal(6, inv.Get(_iron));
        }

        [Fact]
        public void Add_ShouldNormalizeTinyValuesToZero()
        {
            var inv = new Inventory();

            inv.Add(new[] { new Quantity(_iron, 1e-8) });

            Assert.Equal(0, inv.Get(_iron));
        }

        [Fact]
        public void Items_WithSameId_ShouldBeEqual()
        {
            var a = new Item("iron", "Iron A", ItemType.Raw);
            var b = new Item("iron", "Iron B", ItemType.Raw);

            Assert.Equal(a, b);
        }
    }
}
