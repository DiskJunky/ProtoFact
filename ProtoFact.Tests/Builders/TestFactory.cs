using ProtoFact.Control;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Abstractions;
using ProtoFact.Tests.Fakes;

namespace ProtoFact.Tests.Builders;

public static class TestFactory
{
    public static (
        ProductionController controller,
        Engine.Engine engine,
        List<Recipe> recipes,
        IInventory inventory,
        Item ore,
        Item plate,
        Item gear)
        CreateBasicSystem()
    {
        var logger = new FakeLogger();
        var inventory = new Inventory();
        var time = new FakeTimeProvider(0.1d);

        // ✅ Use your real implementations
        var adaptive = new ProportionalController();
        var buffer = new TimeWindowBufferStrategy();
        var solver = new RateSolver();

        var controller = new ProductionController(
                                                  solver,
                                                  adaptive,
                                                  buffer,
                                                  inventory,
                                                  logger);

        // ✅ Items (match Program.cs)
        var ore = new Item("ore", "Ore", ItemType.Raw);
        var plate = new Item("plate", "Plate", ItemType.Intermediate);
        var gear = new Item("gear", "Gear", ItemType.Intermediate);

        // ✅ Recipes (match Program.cs)
        var recipes = new List<Recipe>
                      {
                          new Recipe(new[] { new Quantity(ore, 1) }, new Quantity(plate, 1), 2.0),
                          new Recipe(new[] { new Quantity(plate, 2) }, new Quantity(gear, 1), 2.0),
                          new Recipe(Array.Empty<Quantity>(), new Quantity(ore, 1), 2.0)
                      };

        var engine = new Engine.Engine(controller.Processors, time);

        return (controller, engine, recipes, inventory, ore, plate, gear);
    }
}