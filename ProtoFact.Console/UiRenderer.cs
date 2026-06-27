using Spectre.Console;
using ProtoFact.Engine;
using ProtoFact.Domain;
using System.Collections.Generic;
using ProtoFact.Abstractions;
using Spectre.Console.Rendering;

namespace ProtoFact.Console
{
    /// <summary>
    /// Responsible for rendering the simulation state to the console.
    /// </summary>
    public class UiRenderer
    {
        private readonly IInventory _inventory;
        private readonly IEnumerable<IProcessor> _processors;
        private readonly IProductionController _controller;
        private readonly IEnumerable<Recipe> _recipes;

        public UiRenderer(IInventory inventory, 
                          IEnumerable<IProcessor> processors,
                          IProductionController controller,
                          IEnumerable<Recipe> recipes)
        {
            _inventory = inventory;
            _processors = processors;
            _controller = controller;
            _recipes = recipes;
        }

        public IRenderable Render(IReadOnlyList<Item> trackedItems)
        {
            var footer = new Panel(BuildFooter(trackedItems))
                                .Border(BoxBorder.Rounded);
            footer.Width = 70;
            var layout = new Rows(
                                  new Panel(BuildMetricsTable(trackedItems))
                                      .Header("System Overview")
                                      .Border(BoxBorder.Rounded),

                                  footer,

                                  new Panel(BuildProcessorTable())
                                      .Header("Processors")
                                      .Border(BoxBorder.Rounded)
                                 );

            return layout;
        }

        private Table BuildProcessorTable()
        {
            var table = new Table()
                        .Border(TableBorder.Simple)
                        .AddColumn("Item")
                        .AddColumn("Running")
                        .AddColumn("Idle")
                        .AddColumn("Util");

            var groups = _processors
                .GroupBy(p => p.Recipe.Output.Item);

            foreach (var g in groups)
            {
                var total = g.Count();
                var running = g.Count(p => p.IsRunning);
                var idle = total - running;

                var util = total == 0 ? 0 : (double)running / total;

                table.AddRow(
                             g.Key.Name,
                             $"[green]{running}[/]",
                             $"[red]{idle}[/]",
                             $"{util:P0}"
                            );
            }

            return table;
        }

        private Table BuildMetricsTable(IReadOnlyList<Item> items)
        {
            var goals = _controller.GetGoals()
                                   .ToDictionary(g => g.Target);

            var table = new Table()
                        .Border(TableBorder.Simple)
                        .AddColumn("Item")
                        .AddColumn("Stock")
                        .AddColumn("Util")
                        .AddColumn("Thru")
                        .AddColumn("Max")
                        .AddColumn("Target")
                        .AddColumn("Δ")
                        .AddColumn("Proc");

            foreach (var item in items)
            {
                var stock = _inventory.Get(item);
                var util = _controller.GetUtilization(item);
                var throughput = _controller.GetThroughput(item);
                var max = _controller.GetMaxThroughput(item, _recipes);
                var count = _processors.Count(p => p.Recipe.Output.Item.Equals(item));

                goals.TryGetValue(item, out var goal);
                var target = goal?.TargetRate ?? 0;

                var delta = throughput - target;

                // ✅ Status colouring
                string deltaText;
                if (target <= 0)
                {
                    deltaText = "-";
                }
                else if (throughput >= target * 0.95)
                {
                    deltaText = $"[green]+{delta:F2}[/]";
                }
                else if (throughput >= target * 0.7)
                {
                    deltaText = $"[yellow]{delta:F2}[/]";
                }
                else
                {
                    deltaText = $"[red]{delta:F2}[/]";
                }

                table.AddRow(
                             item.Name,
                             $"{stock:F1}",
                             $"{util:P0}",
                             $"{throughput:F2}",
                             $"{max:F2}",
                             target > 0 ? $"{target:F2}" : "-",
                             deltaText,
                             count.ToString()
                            );
            }

            return table;
        }

        private IRenderable BuildFooter(IReadOnlyList<Item> items)
        {
            var bottlenecks = _controller.GetTopBottlenecks(3).ToList();
            var metrics = _controller.GetSystemMetrics();
            var goals = _controller.GetGoals().ToList();

            var bottleneckText = bottlenecks.Any()
                ? string.Join(", ", bottlenecks.Select(b =>
                {
                    var color = b.Severity switch
                                {
                                    > 0.7 => "red",
                                    > 0.3 => "yellow",
                                    _ => "green"
                                };

                    return $"[{color}]{b.Item.Name} ({b.Severity:P0})[/]";
                }))
                : "None";

            var goalSummary = goals.Any()
                ? string.Join(", ",
                              goals.Select(g => $"{g.Target.Name}:{g.TargetRate:F1}/s"))
                : "None";

            return new Markup(
                              $"[yellow]Goals:[/] {goalSummary}\n" +
                              $"[yellow]Bottlenecks:[/] {bottleneckText}\n" +
                              $"[yellow]System:[/] Util {metrics.Utilization:P0} | Idle {metrics.IdleFraction:P0}"
                             );
        }
    }
}