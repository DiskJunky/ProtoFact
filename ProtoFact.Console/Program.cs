using Ninject;
using Spectre.Console;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Abstractions;
using ProtoFact.Control;
using ProtoFact.Infrastructure;
using ProtoFact.Scenarios;

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

        var scenario = DemoScenario.Create();
        var recipes = scenario.Recipes;

        var validation = kernel.Get<IModelValidator>().Validate(recipes);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Recipe model failed validation: {string.Join("; ", validation.Errors)}");
        }

        inventory.Add(scenario.InitialStock);

        var rateSolver = kernel.Get<IRateSolver>();
        var controller = new ProductionController(rateSolver,
                                                  adaptiveController,
                                                  bufferStrategy,
                                                  inventory,
                                                  logger);

        foreach (var goal in scenario.Goals)
        {
            controller.AddGoal(goal);
        }

        var time = new RealTimeProvider();
        var engine = new EngineRunner(controller.Processors, time);

        var renderer = new UiRenderer(inventory, 
                                      controller.Processors, 
                                      controller, 
                                      recipes);
        var trackedItems = scenario.TrackedItems;

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
