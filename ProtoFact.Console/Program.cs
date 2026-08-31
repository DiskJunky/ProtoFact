using Ninject;
using Spectre.Console;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Abstractions;
using ProtoFact.Control;
using ProtoFact.Infrastructure;

using EngineRunner = ProtoFact.Engine.Engine;

namespace ProtoFact.Console;

class Program
{
    static void Main()
    {
        try
        {
            Initialize();
        }
        catch (Exception e)
        {
            System.Console.WriteLine("There was a fatal error:");
            var originalForeColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Magenta;
            System.Console.WriteLine(e);

            // restore or all further text will be magenta
            System.Console.ForegroundColor = originalForeColor;
        }
    }

    private static void Initialize()
    {
        var kernel = new StandardKernel(new BindingsModule());

        var logger = kernel.Get<ILogger>();
        var inventory = kernel.Get<IInventory>();
        var adaptiveController = kernel.Get<IAdaptiveController>();
        var bufferStrategy = kernel.Get<IBufferStrategy>();

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
                                   Array.Empty<Quantity>(),     // ✅ no inputs
                                   new Quantity(ore, 1),        // produces ore
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

        var recipes = new[]
                      {
                          oreRecipe,
                          copperOreRecipe,
                          plateRecipe,
                          wireRecipe,
                          gearRecipe,
                          circuitRecipe,
                          robotRecipe,
                      };

        var validation = kernel.Get<IModelValidator>().Validate(recipes);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Recipe model failed validation: {string.Join("; ", validation.Errors)}");
        }

        inventory.Add(new[] { new Quantity(ore, 10), new Quantity(copperOre, 10) });

        var rateSolver = kernel.Get<IRateSolver>();
        var controller = new ProductionController(rateSolver,
                                                  adaptiveController,
                                                  bufferStrategy,
                                                  inventory,
                                                  logger);

        // Goals: one on a shared tier-2 intermediate (gear) and one on the
        // final product (robot), so demand for gear is aggregated across
        // both goals by the rate solver.
        controller.AddGoal(new ProductionGoal(gear, 1.0));
        controller.AddGoal(new ProductionGoal(robot, 0.5));

        var time = new RealTimeProvider();
        var engine = new EngineRunner(controller.Processors, time);

        var renderer = new UiRenderer(inventory, 
                                      controller.Processors, 
                                      controller, 
                                      recipes);
        var trackedItems = new[] { ore, copperOre, plate, wire, gear, circuit, robot };

        //_ = System.Console.ReadKey(true); // Wait for a key press before starting the simulation

        AnsiConsole.Live(renderer.Render(trackedItems))
                   .Start(ctx =>
                   {
                       while (true)
                       {
                           if (System.Console.KeyAvailable &&
                               System.Console.ReadKey(true).Key == System.ConsoleKey.Q)
                               break;

                           controller.Tick(recipes);
                           engine.Tick();

                           ctx.UpdateTarget(renderer.Render(trackedItems));

                           Thread.Sleep(100);
                       }
                   });
    }
}