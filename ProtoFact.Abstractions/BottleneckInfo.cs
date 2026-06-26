using ProtoFact.Domain;

namespace ProtoFact.Abstractions;

public record BottleneckInfo(
    Item Item,
    int TotalProcessors,
    int RunningProcessors,
    double Utilization,
    double Severity
);