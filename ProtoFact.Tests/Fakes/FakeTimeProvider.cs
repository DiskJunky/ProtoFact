using ProtoFact.Abstractions;

namespace ProtoFact.Tests.Fakes
{
    public class FakeTimeProvider : ITimeProvider
    {
        public double DeltaTime { get; set; }
    }
}