namespace ProtoFact.Abstractions
{
    /// <summary>
    /// Provides time progression for the simulation.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the time delta (in seconds) since the last tick.
        /// </summary>
        double DeltaTime { get; }
    }
}
