using Ninject;
using Spectre.Console;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Abstractions;
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

        var recipe = new Recipe(
                                new[] { new Quantity(ore, 1) },
                                new Quantity(plate, 1),
                                1.0);

        inventory.Add(new[] { new Quantity(ore, 10) });

        var processor = new Processor(recipe, inventory, logger);

        var processors = new[] { processor };

        var time = new RealTimeProvider();
        var engine = new EngineRunner(processors, time);

        var renderer = new UiRenderer(inventory, processors);
        var trackedItems = new[] { ore, plate };

        AnsiConsole.Live(renderer.Render(trackedItems))
                   .Start(ctx =>
                   {
                       while (true)
                       {
                           if (System.Console.KeyAvailable &&
                               System.Console.ReadKey(true).Key == System.ConsoleKey.Q)
                               break;

                           engine.Tick();

                           ctx.UpdateTarget(renderer.Render(trackedItems));

                           Thread.Sleep(100);
                       }
                   });
    }
}