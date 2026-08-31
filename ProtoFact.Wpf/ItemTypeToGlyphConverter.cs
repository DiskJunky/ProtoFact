using System;
using System.Globalization;
using System.Windows.Data;
using ProtoFact.Domain;

namespace ProtoFact.Wpf;

/// <summary>
/// Converts an <see cref="ItemType"/> to a display glyph (Segoe Fluent
/// Icons codepoint), used to visually distinguish raw materials,
/// intermediates, and final products in the recipe tree. An icon font is
/// used instead of color emoji because WPF cannot render color (COLR/CPAL)
/// glyphs - it always tints glyphs with the current Foreground brush.
/// </summary>
public sealed class ItemTypeToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ItemType type
            ? type switch
              {
                  ItemType.Raw => "\uE7B8",          // Package
                  ItemType.Intermediate => "\uE713", // Setting/gear
                  ItemType.Final => "\uEB4D",         // Trophy2
                  _ => "\uE734",                      // FavoriteStar
              }
            : "\uE734";
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
