using Spectre.Console;
using ProtoFact.Engine;
using ProtoFact.Domain;
using System.Collections.Generic;
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

        public UiRenderer(IInventory inventory, IEnumerable<IProcessor> processors)
        {
            _inventory = inventory;
            _processors = processors;
        }

        public IRenderable Render(IReadOnlyList<Item> trackedItems)
        {
            var table = new Table()
                        .Border(TableBorder.Rounded)
                        .AddColumn("[yellow]Metric[/]")
                        .AddColumn("[white]Value[/]", c => c.Width = 8);

            // Processors
            foreach (var processor in _processors)
            {
                table.AddRow(
                             "Processor",
                             $"{processor.Recipe.Output.Item.Name}"
                            );

                var stateColor = processor.State == ProcessorState.Running ? "green" : "red";

                table.AddRow(
                             "State",
                             $"[{stateColor}]{processor.State}[/]"
                            );

                table.AddRow(
                             "Progress",
                             $"{processor.Progress:P0}"
                            );

                table.AddEmptyRow();
            }

            // Inventory
            foreach (var item in trackedItems)
            {
                table.AddRow(
                             item.Name,
                             _inventory.Get(item).ToString("F2")
                            );
            }

            return table;
        }
    }
}