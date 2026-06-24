using Spectre.Console;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Abstractions;

using EngineRunner = ProtoFact.Engine.Engine;

namespace ProtoFact.Console;

class Program
{
    static void Main()
    {
        // Items (shared references — IMPORTANT)
        var ore = new Item("ore", "Ore", ItemType.Raw);
        var plate = new Item("plate", "Plate", ItemType.Intermediate);

        // Recipe
        var recipe = new Recipe(
                                new[] { new Quantity(ore, 1) },
                                new Quantity(plate, 1),
                                1.0);

        // Inventory
        var inventory = new Inventory();
        inventory.Add(new[] { new Quantity(ore, 10) });

        // Processor(s)
        var processor = new Processor(recipe, inventory);

        // Time
        var time = new RealTimeProvider();

        // Engine
        var engine = new EngineRunner(new[] { processor }, time);

        // UI
        var renderer = new UiRenderer(inventory, new[] { processor });

        var trackedItems = new[] { ore, plate };

        // Live UI loop
        AnsiConsole.Live(renderer.Render(trackedItems))
                   .Start(ctx =>
                   {
                       while (true)
                       {
                           engine.Tick();

                           ctx.UpdateTarget(renderer.Render(trackedItems));

                           Thread.Sleep(100);
                       }
                   });
    }
}