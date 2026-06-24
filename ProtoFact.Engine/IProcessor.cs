using ProtoFact.Domain;

namespace ProtoFact.Engine
{
    public interface IProcessor
    {
        Recipe Recipe { get; }
        
        ProcessorState State { get; }
        
        double Progress { get; }

        void Tick(double deltaTime);
    }
}