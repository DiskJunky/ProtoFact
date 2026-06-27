using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    public interface IProcessor
    {
        Recipe Recipe { get; }
        
        ProcessorState State { get; }
        
        double Progress { get; }

        bool IsRunning { get; }

        void Tick(double deltaTime);

        bool IsMarkedForRemoval { get; set; }
    }
}