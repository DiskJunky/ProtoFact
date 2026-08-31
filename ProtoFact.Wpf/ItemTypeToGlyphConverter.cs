using System;
using System.Globalization;
using System.Windows.Data;
using ProtoFact.Domain;

namespace ProtoFact.Wpf;

/// <summary>
/// Converts an <see cref="ItemType"/> to a display glyph, used to visually
/// distinguish raw materials, intermediates, and final products in the
/// recipe tree.
/// </summary>
public sealed class ItemTypeToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ItemType type
            ? type switch
              {
                  ItemType.Raw => "\u26CF\uFE0F",
                  ItemType.Intermediate => "\u2699\uFE0F",
                  ItemType.Final => "\U0001F3C6",
                  _ => "\u2022",
              }
            : "\u2022";
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
