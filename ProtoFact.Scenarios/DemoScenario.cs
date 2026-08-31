using System;
using System.Collections.Generic;
using System.Linq;
using ProtoFact.Control;
using ProtoFact.Domain;

namespace ProtoFact.Scenarios
{
    /// <summary>
    /// Shared demo scenario definition: a diamond-shaped recipe DAG (two raw
    /// materials feeding two independent branches that merge into a final
    /// product), plus starting goals/inventory. Used by both the console and
    /// WPF front-ends so recipes/goals stay a single source of truth.
    /// </summary>
    public sealed class DemoScenario
    {
        public IReadOnlyList<Item> TrackedItems { get; }
        public IReadOnlyList<Recipe> Recipes { get; }
        public IReadOnlyList<Quantity> InitialStock { get; }
        public IReadOnlyList<ProductionGoal> Goals { get; }

        /// <summary>
        /// The subset of <see cref="TrackedItems"/> that are raw resources
        /// (i.e. have no recipe inputs, produced by "generator" recipes).
        /// These are the only items intended to be user-adjustable.
        /// </summary>
        public IReadOnlyList<Item> RawItems { get; }

        private DemoScenario(
            IReadOnlyList<Item> trackedItems,
            IReadOnlyList<Recipe> recipes,
            IReadOnlyList<Quantity> initialStock,
            IReadOnlyList<ProductionGoal> goals)
        {
            TrackedItems = trackedItems;
            Recipes = recipes;
            InitialStock = initialStock;
            Goals = goals;
            RawItems = trackedItems.Where(i => i.Type == ItemType.Raw).ToList();
        }

        public static DemoScenario Create()
        {
            // Raw materials
            var ore = new Item("ore", "Ore", ItemType.Raw);
            var copperOre = new Item("copper_ore", "Copper Ore", ItemType.Raw);

            // Tier 1 intermediates (one per raw material)
            var plate = new Item("plate", "Plate", ItemType.Intermediate);
            var wire = new Item("wire", "Wire", ItemType.Intermediate);

            // Tier 2 intermediates
            var gear = new Item("gear", "Gear", ItemType.Intermediate);
            var circuit = new Item("circuit", "Circuit", ItemType.Intermediate);

            // Final product: merges both branches (diamond-shaped DAG)
            var robot = new Item("robot", "Robot", ItemType.Final);

            // Raw resource "generator" recipes (no inputs)
            var oreRecipe = new Recipe(
                                       Array.Empty<Quantity>(),
                                       new Quantity(ore, 1),
                                       2.0);
            var copperOreRecipe = new Recipe(
                                            Array.Empty<Quantity>(),
                                            new Quantity(copperOre, 1),
                                            2.0);

            // Tier 1 recipes
            var plateRecipe = new Recipe(
                                         new[] { new Quantity(ore, 1) },
                                         new Quantity(plate, 1),
                                         2.0);
            var wireRecipe = new Recipe(
                                        new[] { new Quantity(copperOre, 1) },
                                        new Quantity(wire, 1),
                                        2.0);

            // Tier 2 recipes
            var gearRecipe = new Recipe(
                                        new[] { new Quantity(plate, 2) },
                                        new Quantity(gear, 1),
                                        2.0);
            var circuitRecipe = new Recipe(
                                           new[] { new Quantity(wire, 2) },
                                           new Quantity(circuit, 1),
                                           2.0);

            // Final recipe: merges the gear and circuit branches
            var robotRecipe = new Recipe(
                                         new[] { new Quantity(gear, 1), new Quantity(circuit, 1) },
                                         new Quantity(robot, 1),
                                         3.0);

            var recipes = new List<Recipe>
                          {
                              oreRecipe,
                              copperOreRecipe,
                              plateRecipe,
                              wireRecipe,
                              gearRecipe,
                              circuitRecipe,
                              robotRecipe,
                          };

            var trackedItems = new List<Item> { ore, copperOre, plate, wire, gear, circuit, robot };

            var initialStock = new List<Quantity>
                               {
                                   new Quantity(ore, 10),
                                   new Quantity(copperOre, 10),
                               };

            // Goals: one on a shared tier-2 intermediate (gear) and one on
            // the final product (robot), so demand for gear is aggregated
            // across both goals by the rate solver.
            var goals = new List<ProductionGoal>
                       {
                           new ProductionGoal(gear, 1.0),
                           new ProductionGoal(robot, 0.5),
                       };

            return new DemoScenario(trackedItems, recipes, initialStock, goals);
        }
    }
}
