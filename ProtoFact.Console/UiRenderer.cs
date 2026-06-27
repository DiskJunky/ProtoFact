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

        public UiRenderer(IInventory inventory, 
                          IEnumerable<IProcessor> processors,
                          IProductionController controller)
        {
            _inventory = inventory;
            _processors = processors;
            _controller = controller;
        }

        public IRenderable Render(IReadOnlyList<Item> trackedItems)
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn().NoWrap()); // left
            grid.AddColumn();                          // right

            // LEFT: system
            var metricsPanel = new Panel(BuildMetricsTable(trackedItems))
                               .Header("System")
                               .Border(BoxBorder.Rounded);

            // RIGHT: processors
            var processorPanel = new Panel(BuildProcessorTable())
                                 .Header("Processors")
                                 .Border(BoxBorder.Rounded);

            grid.AddRow(metricsPanel, processorPanel);

            return grid;
        }
        private Table BuildProcessorTable()
        {
            var table = new Table()
                        .Border(TableBorder.Simple)
                        .AddColumn("Item")
                        .AddColumn("State", c => c.Width = 8)
                        .AddColumn("Progress");

            foreach (var p in _processors)
            {
                var stateColor = p.State == ProcessorState.Running ? "green" : "red";

                table.AddRow(
                             p.Recipe.Output.Item.Name,
                             $"[{stateColor}]{p.State}[/]",
                             $"{p.Progress:P0}"
                            );
            }

            return table;
        }

        private Table BuildMetricsTable(IReadOnlyList<Item> items)
        {
            var table = new Table()
                        .Border(TableBorder.Simple)
                        .AddColumn("Metric")
                        .AddColumn("Value", c => c.Width = 24);

            // ---- Inventory
            table.AddRow("[yellow]Inventory[/]", "");

            foreach (var item in items)
            {
                table.AddRow(item.Name, _inventory.Get(item).ToString("F2"));
            }

            table.AddEmptyRow();

            // ---- Utilization
            table.AddRow("[yellow]Utilization[/]", "");

            foreach (var item in items)
            {
                var util = _controller.GetUtilization(item);
                table.AddRow(item.Name, $"{util:P0}");
            }

            table.AddEmptyRow();

            // ---- Throughput
            table.AddRow("[yellow]Throughput[/]", "");

            foreach (var item in items)
            {
                var throughput = _controller.GetThroughput(item);
                table.AddRow(item.Name, $"{throughput:F2}/s");
            }

            table.AddEmptyRow();

            // ---- Process counts
            table.AddRow("[yellow]Processors[/]", "");

            foreach (var item in items)
            {
                var count = _processors.Count(p => p.Recipe.Output.Item.Equals(item));
                table.AddRow(item.Name, count.ToString());
            }

            table.AddEmptyRow();

            // ---- System metrics
            var metrics = _controller.GetSystemMetrics();

            table.AddRow("[yellow]System[/]", "");
            table.AddRow("Utilization", $"{metrics.Utilization:P0}");
            table.AddRow("Idle", $"{metrics.IdleFraction:P0}");

            table.AddEmptyRow();

            // ---- Bottlenecks
            var bottlenecks = _controller.GetTopBottlenecks(3).ToList();

            table.AddRow(
                         "[yellow]Bottlenecks[/]",
                         bottlenecks.Any()
                             ? string.Join(", ",
                                           bottlenecks.Select(b =>
                                           {
                                               var color = b.Severity switch
                                                           {
                                                               > 0.7 => "red",
                                                               > 0.3 => "yellow",
                                                               _ => "green"
                                                           };

                                               return $"[{color}]{b.Item.Name} ({b.Severity:P0})[/]";
                                           }))
                             : "None"
                        );

            return table;
        }
    }
}