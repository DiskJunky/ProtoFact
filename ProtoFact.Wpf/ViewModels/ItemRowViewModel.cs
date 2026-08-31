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
    /// Glyph representing the item's type (raw material, intermediate, or
    /// final product), shown as a prefix in the overview grid.
    /// </summary>
    public string TypeGlyph => Item.Type switch
    {
        ItemType.Raw => "\u26CF\uFE0F",          // pick (raw material)
        ItemType.Intermediate => "\u2699\uFE0F", // gear (intermediate)
        ItemType.Final => "\U0001F3C6",          // trophy (final product)
        _ => "\u2022",
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

    private string _statusGlyph = "\u2796"; // heavy minus sign (no goal)
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
            StatusGlyph = "\u2796";       // heavy minus sign
            StatusColor = Brushes.Gray;
        }
        else if (Throughput >= TargetRate * 0.95)
        {
            StatusGlyph = "\u2705";       // check mark
            StatusColor = Brushes.SeaGreen;
        }
        else if (Throughput >= TargetRate * 0.7)
        {
            StatusGlyph = "\u26A0\uFE0F"; // warning
            StatusColor = Brushes.DarkGoldenrod;
        }
        else
        {
            StatusGlyph = "\U0001F534";   // red circle
            StatusColor = Brushes.Crimson;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
