using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ProtoFact.Domain;

namespace ProtoFact.Wpf.ViewModels;

/// <summary>
/// A single row in the system-overview grid, mirroring the console UI's
/// metrics table (stock/throughput/utilization/target/delta per item).
/// </summary>
public sealed class ItemRowViewModel : INotifyPropertyChanged
{
    public Item Item { get; }
    public string Name => Item.Name;

    /// <summary>
    /// Icon-font glyph (Segoe Fluent Icons) representing the item's type
    /// (raw material, intermediate, or final product), shown as a prefix
    /// in the overview grid. Uses an icon font rather than color emoji
    /// because WPF's text stack cannot render color (COLR/CPAL) glyphs -
    /// it always tints glyphs with the current Foreground.
    /// </summary>
    public string TypeGlyph => Item.Type switch
    {
        ItemType.Raw => "\uE7B8",          // Package (raw material)
        ItemType.Intermediate => "\uE713", // Setting/gear (intermediate)
        ItemType.Final => "\uEB4D",        // Trophy2 (final product)
        _ => "\uE734",                     // FavoriteStar fallback
    };

    /// <summary>Color used to tint <see cref="TypeGlyph"/>, matching the recipe tree.</summary>
    public Brush TypeColor => Item.Type switch
    {
        ItemType.Raw => ThemeManager.Brush("TypeRawBrush"),
        ItemType.Intermediate => ThemeManager.Brush("TypeIntermediateBrush"),
        ItemType.Final => ThemeManager.Brush("TypeFinalBrush"),
        _ => ThemeManager.Brush("StatusNoneBrush"),
    };

    private double _stock;
    public double Stock
    {
        get => _stock;
        set => SetField(ref _stock, value);
    }

    private double _utilization;
    public double Utilization
    {
        get => _utilization;
        set => SetField(ref _utilization, value);
    }

    private double _throughput;
    public double Throughput
    {
        get => _throughput;
        set => SetField(ref _throughput, value);
    }

    private double _maxThroughput;
    public double MaxThroughput
    {
        get => _maxThroughput;
        set => SetField(ref _maxThroughput, value);
    }

    private double _targetRate;
    public double TargetRate
    {
        get => _targetRate;
        set => SetField(ref _targetRate, value);
    }

    private double _delta;
    public double Delta
    {
        get => _delta;
        set => SetField(ref _delta, value);
    }

    private int _processorCount;
    public int ProcessorCount
    {
        get => _processorCount;
        set => SetField(ref _processorCount, value);
    }

    private string _statusGlyph = "\uE738"; // Remove (no goal)
    public string StatusGlyph
    {
        get => _statusGlyph;
        set => SetField(ref _statusGlyph, value);
    }

    private Brush _statusColor = Brushes.Gray;
    public Brush StatusColor
    {
        get => _statusColor;
        set => SetField(ref _statusColor, value);
    }

    public ItemRowViewModel(Item item)
    {
        Item = item;
    }

    /// <summary>
    /// Recomputes <see cref="StatusGlyph"/>/<see cref="StatusColor"/> from
    /// the current throughput vs. target, using the same thresholds as the
    /// console dashboard (95%/70% of target).
    /// </summary>
    public void UpdateStatus()
    {
        if (TargetRate <= 0)
        {
            StatusGlyph = "\uE738";       // Remove
            StatusColor = ThemeManager.Brush("StatusNoneBrush");
        }
        else if (Throughput >= TargetRate * 0.95)
        {
            StatusGlyph = "\uE930";       // Completed (check mark)
            StatusColor = ThemeManager.Brush("StatusOkBrush");
        }
        else if (Throughput >= TargetRate * 0.7)
        {
            StatusGlyph = "\uE7BA";       // Warning
            StatusColor = ThemeManager.Brush("StatusWarnBrush");
        }
        else
        {
            StatusGlyph = "\uEB90";       // StatusErrorFull
            StatusColor = ThemeManager.Brush("StatusBadBrush");
        }
    }

    /// <summary>
    /// Re-resolves theme-dependent brush properties after the active theme
    /// changes, since plain C# properties can't use DynamicResource and
    /// must be recomputed explicitly.
    /// </summary>
    public void RefreshTheme()
    {
        OnPropertyChanged(nameof(TypeColor));
        UpdateStatus();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName ?? string.Empty);
    }
}
