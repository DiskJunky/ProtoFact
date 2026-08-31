using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ProtoFact.Domain;

namespace ProtoFact.Wpf;

/// <summary>
/// Converts an <see cref="ItemType"/> to a display color, used to tint the
/// type glyph consistently across the overview grid and recipe tree.
/// Resolves the brush from the active theme's resource dictionary so it
/// respects light/dark mode.
/// </summary>
public sealed class ItemTypeToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ItemType type
            ? type switch
              {
                  ItemType.Raw => ThemeManager.Brush("TypeRawBrush"),
                  ItemType.Intermediate => ThemeManager.Brush("TypeIntermediateBrush"),
                  ItemType.Final => ThemeManager.Brush("TypeFinalBrush"),
                  _ => Brushes.Gray,
              }
            : Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
