# ProtoFact Roadmap

Prioritized list of next steps identified during the architecture review.
Items are ordered roughly by priority; check items off / update status as
work lands.

## 1. Expand recipe model complexity (in progress this session)

**Why**: The current console demo only has 3 items / 3 recipes (ore -> plate
-> gear). This is too shallow to properly exercise the DAG solver's
branch-sharing, `ModelValidator`'s cycle detection, and the controller's
stability guards under real contention (e.g., two outputs competing for the
same intermediate).

**Scope**:
- Add a second raw material and a branch that merges two intermediates into
  one final product (a "diamond" shape in the DAG), so shared-dependency
  aggregation in `RateSolver.BuildNode` is actually exercised by the demo.
- Add corresponding `ProductionGoal`s and tracked items for the UI.
- Resolve the `ModelValidator` zero-input-recipe gap (see
  [ARCHITECTURE.md](./ARCHITECTURE.md#key-design-notes--known-nuances)) so
  validation can run cleanly against raw-resource generator recipes, and
  wire `ModelValidator` into the console startup (or a test) so regressions
  are caught.

## 2. Config/scenario-driven recipes

**Why**: Recipes and goals are hardcoded in `Program.cs`. Externalizing them
(e.g., to JSON) would let different factory scenarios be tried without
recompiling, and would make the "expand recipe complexity" work sustainable
as the model grows.

**Scope**: Define a serializable scenario format (items, recipes, goals),
load it at startup, keep the current hardcoded set as a fallback/example
scenario file.

## 3. Adaptive controller tuning & tests

**Why**: `IAdaptiveController` / `ProportionalController` suggest a PID-style
control loop, but current test coverage for adaptive tuning behavior
(vs. the higher-level `ProductionController` stability tests) is thin.

**Scope**: Add focused unit tests around `ProportionalController` gain
behavior, consider exposing tunable parameters (proportional gain, etc.)
rather than hardcoded constants, and document expected tuning ranges.

## 4. Documentation upkeep

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
