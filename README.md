# ProtoFact
Semi-automatic factory simulater

![console-sample.gif](./docs/images/console-sample.gif)

## Current state

ProtoFact resolves a recipe DAG for one or more production goals, scales
processors (machines) up/down each tick to hit target throughput while
respecting inventory buffers, and renders a live terminal dashboard
(stock, throughput, utilization, bottlenecks) via Spectre.Console. The
solution builds cleanly on .NET 10 and has 41 passing unit tests covering
the solver, inventory, processor lifecycle, controller stability, and
model validation.

See:
- [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md) - project structure, control-loop flow, and known design nuances.
- [docs/ROADMAP.md](./docs/ROADMAP.md) - prioritized next steps.
