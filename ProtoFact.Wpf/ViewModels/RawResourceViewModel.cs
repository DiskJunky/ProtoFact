using System;
using ProtoFact.Domain;
using ProtoFact.Engine;

namespace ProtoFact.Wpf.ViewModels;

/// <summary>
/// A raw-resource adjustment row: lets the user "find" more of a base
/// resource by adding a chosen amount straight into inventory. Recipes are
/// fixed; only raw resource stock is user-adjustable.
/// </summary>
public sealed class RawResourceViewModel
{
    public Item Item { get; }
    public string Name => Item.Name;

    /// <summary>Pick glyph - all rows here are raw materials by construction.</summary>
    public string Glyph => "\u26CF\uFE0F";

    public double AmountToAdd { get; set; } = 10;

    public RelayCommand AddCommand { get; }

    public RawResourceViewModel(Item item, IInventory inventory)
    {
        Item = item;

        AddCommand = new RelayCommand(() =>
        {
            if (AmountToAdd <= 0)
                return;

            inventory.Add(new[] { new Quantity(item, AmountToAdd) });
        });
    }
}
