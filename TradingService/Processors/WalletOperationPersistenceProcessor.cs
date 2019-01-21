using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors
{
    public class WalletOperationPersistenceProcessor
    {
        public ActivityHistoryService ActivityHistoryService { get; }

        /// <summary>
        /// Created via ProcessorFactory.
        /// </summary>
        public WalletOperationPersistenceProcessor(ActivityHistoryService activityHistoryService)
        {
            ActivityHistoryService = activityHistoryService;
        }

        public Task PersistWalletOperation(string user, string accountId, string coinSymbol, string depositType,
            string withdrawalType, decimal amount, Func<string, Task> reportInvalidMessage)
        {
            return ActivityHistoryService.PersistWalletOperation(user, accountId, coinSymbol, depositType,
                withdrawalType, amount);
        }
    }
}
