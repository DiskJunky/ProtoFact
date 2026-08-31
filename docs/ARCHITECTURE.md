# ProtoFact Architecture

_Last updated: current as of the "expanded recipe model" work._

## Purpose

ProtoFact is a semi-automatic factory production simulator. Given one or more
production **goals** (an item + a target rate), the system:

1. Resolves the recipe DAG needed to produce the goal item.
2. Continuously scales the number of active "processors" (machines) per item
   up or down to try to hit the target throughput, without overshooting
   inventory buffers or oscillating.
3. Advances processor state each tick (consuming inputs, producing outputs).
4. Renders a live terminal dashboard (stock, throughput, utilization,
   bottlenecks) via Spectre.Console.

## Project Map

| Project | Responsibility |
|---|---|
| `ProtoFact.Domain` | Core value types: `Item`, `Quantity`, `Recipe`. No dependencies on other ProtoFact projects. |
| `ProtoFact.Abstractions` | Interfaces and DTOs shared across layers: `IRateSolver`, `ISolver`, `IProductionController`, `IProductionGoal`, `IBufferStrategy`, `IAdaptiveController`, `IProcessor`, `IModelValidator`, `ITimeProvider`, `ILogger`, plus `ProductionNode`, `ProcessorState`, `SystemMetrics`, `BottleneckInfo`, `ValidationResult`. |
| `ProtoFact.Engine` | Core simulation logic: `Engine` (tick loop over processors), `Inventory` (stock, atomic consume), `RateSolver`/`Solver` (recipe-graph resolution into a `ProductionNode` DAG), `Processor` (per-machine state machine: idle -> running -> complete), `ModelValidator` (structural + cycle validation of a recipe set), `ProportionalController` (adaptive control), `TimeWindowBufferStrategy` (target buffer sizing), `MathUtil`. |
| `ProtoFact.Control` | `ProductionController` - orchestrates goals: calls `IRateSolver` per goal, walks the resulting `ProductionNode` DAG, and decides how many processors of each item should exist based on stock deficit, current throughput vs. required throughput, and stability guards. Also owns `ProductionGoal`. |
| `ProtoFact.Infrastructure` | DI wiring (`BindingsModule`, using Ninject) and `NLogLogger` (`ILogger` implementation). |
| `ProtoFact.Console` | Entry point (`Program.cs`) that builds a demo recipe set/goals and runs the live loop; `RealTimeProvider` (`ITimeProvider` wall-clock impl); `UiRenderer` (Spectre.Console dashboard: system overview table, footer with goals/bottlenecks/system metrics, processor table). |
| `ProtoFact.Tests` | xUnit tests for solver, rate solver, inventory, processor, controller (including dedicated stability tests), model validator, engine. Uses `Builders/TestFactory` and `Builders/ItemBuilder` for test data, and `Fakes/FakeLogger`, `Fakes/FakeTimeProvider`. |

## Control-Loop Flow (per tick)

```
Program.Main loop
  -> ProductionController.Tick(recipes)
	   for each ProductionGoal:
		 -> IRateSolver.SolveRate(target, targetRate, recipes)
			  builds a ProductionNode DAG (RateSolver.BuildNode), accumulating
			  required rate and MachinesRequired per item, sharing nodes
			  across goals via a nodeMap keyed by Item (so demand for a
			  shared intermediate is combined, not duplicated).
		 -> ProductionController.ApplyPlanInternal(node, recipes, visited)
			  recurses into node.Inputs FIRST (produce upstream before
			  downstream), then for the current node:
				- looks up its Recipe
				- compares currentStock vs. IBufferStrategy.GetTargetBuffer
				- compares currentThroughput vs. node.RequiredRate
				- applies a deadband (10% of target buffer) to avoid chasing
				  noise, and a hard floor so processor count never drops
				  below the minimum required to satisfy the rate
				- adds/removes IProcessor instances accordingly
  -> Engine.Tick()
	   advances every IProcessor by the elapsed delta time (consume inputs
	   on start, produce output on completion)
  -> UiRenderer.Render(trackedItems)
	   reads IInventory, IProductionController metrics
	   (GetUtilization/GetThroughput/GetMaxThroughput/GetTopBottlenecks/
	   GetSystemMetrics) and the processor list to draw the dashboard
```

## Key Design Notes / Known Nuances

- **Shared DAG nodes**: `RateSolver.BuildNode` keys nodes by `Item` in a
  shared `nodeMap`, so if two goals (or two recipes) depend on the same
  intermediate, the demand is summed onto a single `ProductionNode` rather
  than being double-counted. `ProductionController.ApplyPlanInternal` also
  guards against revisiting the same node twice per tick via a `visited`
  set.
- **Stability guards**: `ProductionController` deliberately avoids
  reacting to small buffer deviations (10% deadband) and never scales a
  required item's processor count below what's needed to hit its target
  rate ("hard floor"). These are covered by
  `ProtoFact.Tests/ProductionControllerStabilityTests.cs`.
- **`ModelValidator` and zero-input recipes**: `ModelValidator.ValidateBasic`
  currently flags any recipe with no inputs as an error
  (`"Recipe '{recipe}' has no inputs."`). However, the demo in
  `Program.cs` defines raw-resource "generator" recipes with
  `Array.Empty<Quantity>()` inputs (e.g., producing ore from nothing) so the
  solver can treat raw materials uniformly. **`ModelValidator` is not
  currently invoked from `Program.cs`**, so this conflict is latent. This is
  called out explicitly because it will surface as soon as validation is
  wired into the demo or a richer recipe set is validated - see the
  roadmap for how this is addressed.
- **DI**: `Program.cs` resolves `ILogger`, `IInventory`,
  `IAdaptiveController`, `IBufferStrategy`, and `IRateSolver` via a Ninject
  `StandardKernel(new BindingsModule())`. `ProductionController` itself is
  constructed manually (not resolved from the kernel).

## Test Coverage Snapshot

41 xUnit tests across: `SolverTests`, `RateSolverTests`, `InventoryTests`,
`ProcessorTests`, `ProductionControllerTests`,
`ProductionControllerStabilityTests`, `ModelValidatorTests`, `EngineTests`.
All passing as of this writing.

See [ROADMAP.md](./ROADMAP.md) for planned next steps.
