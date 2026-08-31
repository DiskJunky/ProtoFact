using System.Collections.Generic;
using System.Linq;
using ProtoFact.Abstractions;
using ProtoFact.Control;
using ProtoFact.Domain;
using ProtoFact.Engine;
using Xunit;

namespace ProtoFact.Tests
{
    public class ProductionTreeBuilderTests
    {
        private sealed class NoStockInventory : IInventorySnapshot
        {
            public double Get(Item item) => 0;
        }

        [Fact]
        public void Should_Flag_Shared_Items_Used_By_Multiple_Edges()
        {
            var a = new Item("a", "A", ItemType.Raw);
            var mid = new Item("mid", "Mid", ItemType.Intermediate);
            var final1 = new Item("final1", "Final1", ItemType.Final);
            var final2 = new Item("final2", "Final2", ItemType.Final);

            var midRecipe = new Recipe(new[] { new Quantity(a, 1) }, new Quantity(mid, 1), 1);
            var final1Recipe = new Recipe(new[] { new Quantity(mid, 1) }, new Quantity(final1, 1), 1);
            var final2Recipe = new Recipe(new[] { new Quantity(mid, 1) }, new Quantity(final2, 1), 1);

            var recipes = new List<Recipe> { midRecipe, final1Recipe, final2Recipe };
            var goals = new List<ProductionGoal>
                       {
                           new ProductionGoal(final1, 1.0),
                           new ProductionGoal(final2, 1.0),
                       };

            var builder = new ProductionTreeBuilder();

            var trees = builder.BuildTrees(goals, recipes, new NoStockInventory());

            Assert.Equal(2, trees.Count);

            var midNodes = trees.SelectMany(FlattenAll).Where(n => n.Item.Equals(mid)).ToList();
            Assert.Equal(2, midNodes.Count);
            Assert.All(midNodes, n => Assert.True(n.IsShared));
            Assert.All(midNodes, n => Assert.Equal(2, n.AggregateRate));
            Assert.All(midNodes, n => Assert.Equal(1, n.EdgeRate));

            var aNodes = trees.SelectMany(FlattenAll).Where(n => n.Item.Equals(a)).ToList();
            Assert.Equal(2, aNodes.Count);
            Assert.All(aNodes, n => Assert.True(n.IsShared));
            Assert.All(aNodes, n => Assert.Equal(2, n.AggregateRate));
        }

        [Fact]
        public void Should_Not_Flag_NonShared_Items()
        {
            var ore = new Item("ore", "Ore", ItemType.Raw);
            var plate = new Item("plate", "Plate", ItemType.Intermediate);

            var recipes = new List<Recipe>
                          {
                              new Recipe(new[] { new Quantity(ore, 1) }, new Quantity(plate, 1), 1),
                          };
            var goals = new List<ProductionGoal> { new ProductionGoal(plate, 1.0) };

            var builder = new ProductionTreeBuilder();

            var trees = builder.BuildTrees(goals, recipes, new NoStockInventory());

            var oreNode = trees.SelectMany(FlattenAll).Single(n => n.Item.Equals(ore));
            Assert.False(oreNode.IsShared);
            Assert.Equal(1, oreNode.EdgeRate);
            Assert.Equal(1, oreNode.AggregateRate);
        }

        private static IEnumerable<ProductionTreeNode> FlattenAll(ProductionTreeNode node)
        {
            yield return node;

            foreach (var input in node.Inputs)
            {
                foreach (var descendant in FlattenAll(input))
                {
                    yield return descendant;
                }
            }
        }
    }
}
