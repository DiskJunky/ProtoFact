using ProtoFact.Abstractions;

namespace ProtoFact.Tests.Fakes
{
    public class FakeTimeProvider : ITimeProvider
    {
        public FakeTimeProvider(double deltaTime = 0d)
        {
            DeltaTime = deltaTime;
        }

        public double DeltaTime { get; set; }
    }
}