using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ProtoFact.Domain;

namespace ProtoFact.Wpf;

/// <summary>
/// Converts an <see cref="ItemType"/> to a display color, used to tint the
/// type glyph consistently across the overview grid and recipe tree.
/// </summary>
public sealed class ItemTypeToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ItemType type
            ? type switch
              {
                  ItemType.Raw => Brushes.SaddleBrown,
                  ItemType.Intermediate => Brushes.SteelBlue,
                  ItemType.Final => Brushes.DarkGoldenrod,
                  _ => Brushes.Gray,
              }
            : Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
