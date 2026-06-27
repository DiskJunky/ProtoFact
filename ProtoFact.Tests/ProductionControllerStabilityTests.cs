using ProtoFact.Control;
using ProtoFact.Domain;
using ProtoFact.Engine;
using ProtoFact.Tests.Builders;

namespace ProtoFact.Tests;

public class ProductionControllerStabilityTests
{
    [Fact]
    public void Should_Not_Stall_With_Zero_Throughput_When_Demand_Exists()
    {
        var (controller, engine, recipes, inventory, ore, plate, gear) = TestFactory.CreateBasicSystem();

        controller.AddGoal(new ProductionGoal(gear, 1.0));
        inventory.Add(new List<Quantity> { new Quantity(ore, 20)});

        RunTicks(engine, controller, recipes, 200);

        var plateThroughput = controller.GetThroughput(plate);

        Assert.True(plateThroughput > 0.01,
            $"Plate throughput should be > 0 but was {plateThroughput}");
    }

    [Fact]
    public void Should_Keep_At_Least_One_Processor_Running_When_Demand_Exists()
    {
        var (controller, engine, recipes, inventory, ore, plate, gear) = TestFactory.CreateBasicSystem();

        controller.AddGoal(new ProductionGoal(gear, 1.0));
        inventory.Add(new List<Quantity> { new Quantity(ore, 20) });

        RunTicks(engine, controller, recipes, 200);

        var plateProcessors = controller.Processors
            .Where(p => p.Recipe.Output.Item.Equals(plate))
            .ToList();

        var running = plateProcessors.Count(p => p.IsRunning);

        Assert.True(running > 0,
            $"Expected some Plate processors running, but got {running}/{plateProcessors.Count}");
    }

    [Fact]
    public void Should_Not_Converge_To_All_Idle_State()
    {
        var (controller, engine, recipes, inventory, ore, plate, gear) = TestFactory.CreateBasicSystem();

        controller.AddGoal(new ProductionGoal(gear, 1.0));
        inventory.Add(new List<Quantity>{ new Quantity(ore, 20) });

        RunTicks(engine, controller, recipes, 300);

        var anyRunning = controller.Processors.Any(p => p.IsRunning);

        Assert.True(anyRunning, "System converged to all processors idle");
    }

    [Fact]
    public void Should_Not_Allow_Throughput_Zero_While_Demand_Positive()
    {
        var (controller, engine, recipes, inventory, ore, plate, gear) = TestFactory.CreateBasicSystem();

        controller.AddGoal(new ProductionGoal(plate, 2.0));
        inventory.Add(new List<Quantity> { new Quantity(ore, 50)});

        RunTicks(engine, controller, recipes, 300);

        var throughput = controller.GetThroughput(plate);
        var goals = controller.GetGoals();

        var requiredRate = goals
        .Where(g => g.Target.Equals(plate))
        .Sum(g => g.TargetRate);

        Assert.False(requiredRate > 0 && throughput == 0,
        $"Invalid steady state: Required={requiredRate}, Throughput={throughput}");
    }

    private static void RunTicks(
        Engine.Engine engine,
        ProductionController controller,
        IEnumerable<Recipe> recipes,
        int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            controller.Tick(recipes);
            engine.Tick();
        }
    }
}
