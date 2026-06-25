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
        var kernel = new StandardKernel(new BindingsModule());

        var logger = kernel.Get<ILogger>();
        var inventory = kernel.Get<IInventory>();

        var ore = new Item("ore", "Ore", ItemType.Raw);
        var plate = new Item("plate", "Plate", ItemType.Intermediate);
        var gear = new Item("gear", "Gear", ItemType.Intermediate);

        var recipe = new Recipe(
                                new[] { new Quantity(ore, 1) },
                                new Quantity(plate, 1),
                                1.0);
        var gearRecipe = new Recipe(
                                    new[] { new Quantity(plate, 2) },
                                    new Quantity(gear, 1),
                                    1.0);
        var oreRecipe = new Recipe(
                                   Array.Empty<Quantity>(),     // ✅ no inputs
                                   new Quantity(ore, 1),        // produces ore
                                   1.0);
        var recipes = new[]
                      {
                          recipe, 
                          gearRecipe,
                          oreRecipe,
                      };

        inventory.Add(new[] { new Quantity(ore, 10) });

        var rateSolver = kernel.Get<IRateSolver>();
        var controller = new ProductionController(rateSolver, inventory, logger);

        // Example goal
        controller.AddGoal(new ProductionGoal(gear, 1.0));

        var time = new RealTimeProvider();
        var engine = new EngineRunner(controller.Processors, time);

        var renderer = new UiRenderer(inventory, controller.Processors, controller);
        var trackedItems = new[] { ore, plate, gear };

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