using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.QueueAccess;

namespace XchangeCrypt.Backend.ConvergenceService.Services
{
    /// <summary>
    /// TradingService instance of QueueWriter.
    /// </summary>
    public class TradingQueueWriter : QueueWriter
    {
        public TradingQueueWriter(
            IConfiguration configuration,
            ILogger<TradingQueueWriter> logger)
            : base(
                configuration["Queue:ConnectionString"],
                configuration["Queue:Trading:Name"],
                logger)
        {
        }
    }
}
