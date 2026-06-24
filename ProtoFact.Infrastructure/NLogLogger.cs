using NLog;
using ILogger = ProtoFact.Abstractions.ILogger;

namespace ProtoFact.Infrastructure
{
    /// <summary>
    /// NLog-backed logger implementation.
    /// </summary>
    public class NLogLogger : ILogger
    {
        private readonly Logger _logger;

        public NLogLogger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Trace(string message) => _logger.Trace(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Info(message);
        public void Error(string message) => _logger.Error(message);
        public void Fatal(string message) => _logger.Fatal(message);
    }
}