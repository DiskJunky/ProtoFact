using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public ItemRowViewModel(Item item)
    {
        Item = item;
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
