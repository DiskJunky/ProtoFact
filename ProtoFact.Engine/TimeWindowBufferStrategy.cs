using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine;

public sealed class TimeWindowBufferStrategy : IBufferStrategy
{
    private readonly double _timeWindowSeconds;

    public TimeWindowBufferStrategy(double timeWindowSeconds = 5)
    {
        _timeWindowSeconds = timeWindowSeconds;
    }

    public double GetTargetBuffer(Item item, double rate)
    {
        return rate * _timeWindowSeconds;
    }
}