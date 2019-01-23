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
            ILogger<TradingQueueWriter> logger
        ) : base(
            configuration["Queue:Trading:ConnectionString"],
            configuration["Queue:Trading:Name"],
            logger)
        {
        }
    }
}
