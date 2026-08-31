# ProtoFact Roadmap

Prioritized list of next steps identified during the architecture review.
Items are ordered roughly by priority; check items off / update status as
work lands.

## 1. Expand recipe model complexity (done)

**Why**: The console demo originally only had 3 items / 3 recipes (ore ->
plate -> gear). This was too shallow to properly exercise the DAG solver's
branch-sharing, `ModelValidator`'s cycle detection, and the controller's
stability guards under real contention (e.g., two outputs competing for the
same intermediate).

**Status**: Done. The demo now has a diamond-shaped DAG (two raw materials
-> two independent tier-1/tier-2 branches -> a merged final product), with
goals on both a shared intermediate and the final product so cross-goal
demand aggregation is exercised. The `ModelValidator` zero-input-recipe gap
was resolved (raw resources may have zero-input "generator" recipes) and
validation is now wired into startup. The scenario definition was extracted
into a shared `ProtoFact.Scenarios` project (`DemoScenario`) so it isn't
duplicated between front-ends.

## 2. WPF app for adjusting raw resource quantities (done)

**Why**: The Spectre.Console dashboard is read-only and keyboard-only
(quit-on-`Q`); the next useful capability is letting a user interactively
adjust available *raw* resources at runtime (e.g., "a new resource deposit
was found") while recipes stay fixed.

**Architecture assessment (informed this work)**: The simulation core
already supports this without changes:
- `IInventory.Add(...)` is a plain additive stock mutation; `Inventory` is
  documented as *"Not thread-safe. Owned by engine"*, but that's fine as
  long as all mutations happen on one thread.
- `ProductionController` and `RateSolver` re-read inventory/recompute the
  DAG every `Tick()`, so a stock change is picked up on the very next tick
  with no caching/staleness issues and no engine restart.
- A WPF app naturally satisfies the single-thread constraint by driving the
  simulation with a `DispatcherTimer` on the UI thread, so button-triggered
  `Add` calls and timer-triggered ticks are serialized automatically.

**Status**: Done. Added `ProtoFact.Wpf` (net10.0-windows), which loads the
shared `DemoScenario`, runs the same `ProductionController`/`Engine` tick
loop via `DispatcherTimer`, shows a system-overview grid mirroring the
console dashboard, and provides an "Add" control per raw resource
(`ItemType.Raw` items only - recipes remain fixed).

**Follow-ups** (not yet done):
- Consider a shared "scenario runtime" abstraction (goal/tick/inventory
  orchestration) between `ProtoFact.Console` and `ProtoFact.Wpf` if more
  front-ends are added, to avoid duplicating the DI/tick-loop wiring.
- Add input validation/feedback in the WPF UI (e.g., disable "Add" for
  invalid amounts beyond the current `<= 0` guard).
- Consider adding automated UI or view-model-level tests once the WPF layer
  grows beyond this initial scope.

## 3. Config/scenario-driven recipes

**Why**: Recipes and goals are hardcoded in `Program.cs`. Externalizing them
(e.g., to JSON) would let different factory scenarios be tried without
recompiling, and would make the "expand recipe complexity" work sustainable
as the model grows.

**Scope**: Define a serializable scenario format (items, recipes, goals),
load it at startup, keep the current hardcoded set as a fallback/example
scenario file.

## 4. Adaptive controller tuning & tests

**Why**: `IAdaptiveController` / `ProportionalController` suggest a PID-style
control loop, but current test coverage for adaptive tuning behavior
(vs. the higher-level `ProductionController` stability tests) is thin.

**Scope**: Add focused unit tests around `ProportionalController` gain
behavior, consider exposing tunable parameters (proportional gain, etc.)
rather than hardcoded constants, and document expected tuning ranges.

## 5. Documentation upkeep

**Why**: `README.md` was minimal (title + one line + gif). Architecture and
roadmap are now captured in `docs/`.

**Status**: Done as part of this session
(`docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, updated `README.md`). Keep these
documents current as the recipe model and control logic evolve.

## Already done / no action needed

- **Bottleneck reporting in the UI**: `BottleneckInfo` is already surfaced -
  `UiRenderer.BuildFooter` calls `IProductionController.GetTopBottlenecks(3)`
  and color-codes them by severity. No further work needed here unless the
  presentation itself needs improvement.
