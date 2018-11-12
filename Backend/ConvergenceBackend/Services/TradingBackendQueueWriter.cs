using Microsoft.Extensions.Configuration;

namespace XchangeCrypt.Backend.ConvergenceBackend.Services
{
    /// <summary>
    /// TradingBackend instance of QueueWriter.
    /// </summary>
    public class TradingBackendQueueWriter : QueueWriter
    {
        /// <summary>
        /// </summary>
        public TradingBackendQueueWriter(IConfiguration configuration) : base(
            configuration["Queue:Trading:ConnectionString"], configuration["Queue:Trading:Name"])
        {
        }
    }
}
