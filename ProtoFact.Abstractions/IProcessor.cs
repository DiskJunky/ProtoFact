using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface IProcessor
    {
        Recipe Recipe { get; }
        
        ProcessorState State { get; }
        
        double Progress { get; }

        void Tick(double deltaTime);
    }
}