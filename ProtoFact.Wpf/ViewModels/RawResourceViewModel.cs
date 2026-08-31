using System;
using System.ComponentModel;
using System.Windows.Media;
using ProtoFact.Domain;
using ProtoFact.Engine;

namespace ProtoFact.Wpf.ViewModels;

/// <summary>
/// A raw-resource adjustment row: lets the user "find" more of a base
/// resource by adding a chosen amount straight into inventory. Recipes are
/// fixed; only raw resource stock is user-adjustable.
/// </summary>
public sealed class RawResourceViewModel : INotifyPropertyChanged
{
    public Item Item { get; }
    public string Name => Item.Name;

    /// <summary>Icon-font glyph - all rows here are raw materials by construction.</summary>
    public string Glyph => "\uE7B8"; // Package

    /// <summary>Color used to tint <see cref="Glyph"/>, matching the recipe tree.</summary>
    public Brush GlyphColor => ThemeManager.Brush("TypeRawBrush");

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

    /// <summary>Re-resolves <see cref="GlyphColor"/> after the active theme changes.</summary>
    public void RefreshTheme()
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GlyphColor)));

    public event PropertyChangedEventHandler? PropertyChanged;
}
