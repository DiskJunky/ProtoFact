namespace ProtoFact.Abstractions;

public record SystemMetrics(
    int TotalProcessors,
    int RunningProcessors,
    double Utilization,   // overall utilisation
    double IdleFraction,  // % idle
    double Saturation     // same as utilisation (alias for clarity)
);