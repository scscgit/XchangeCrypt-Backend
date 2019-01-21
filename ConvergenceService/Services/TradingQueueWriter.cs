using Microsoft.Extensions.Configuration;

namespace XchangeCrypt.Backend.ConvergenceService.Services
{
    /// <summary>
    /// TradingService instance of QueueWriter.
    /// </summary>
    public class TradingQueueWriter : QueueWriter
    {
        /// <summary>
        /// </summary>
        public TradingQueueWriter(IConfiguration configuration) : base(
            configuration["Queue:Trading:ConnectionString"], configuration["Queue:Trading:Name"])
        {
        }
    }
}
