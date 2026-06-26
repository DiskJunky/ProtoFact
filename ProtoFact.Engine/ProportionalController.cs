using ProtoFact.Abstractions;
using ProtoFact.Domain;

namespace ProtoFact.Engine;

public sealed class ProportionalController : IAdaptiveController
{
    private readonly double _kp;
    private readonly double _maxDelta;
    private readonly double _deadband;

    public ProportionalController(double kp = 0.5, double maxDelta = 5, double deadband = 0.5)
    {
        _kp = kp;
        _maxDelta = maxDelta;
        _deadband = deadband;
    }

    public double ComputeAdjustment(Item item, double currentStock, double targetStock)
    {
        var error = targetStock - currentStock;

        if (Math.Abs(error) < _deadband)
            return 0;

        var raw = _kp * error;
        return Math.Clamp(raw, -_maxDelta, _maxDelta);
    }
}