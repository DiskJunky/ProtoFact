namespace ProtoFact.Abstractions
{
    /// <summary>
    /// Application-wide logging abstraction.
    /// </summary>
    public interface ILogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Error(string message);
        void Fatal(string message);
    }
}