using XchangeCrypt.Backend.TradingBackend.Services;

namespace XchangeCrypt.Backend.TradingBackend.Processors
{
    public class ProcessorFactory
    {
        public ActivityHistoryService ActivityHistoryService { get; }
        public TradeExecutor TradeExecutor { get; }

        public ProcessorFactory(ActivityHistoryService activityHistoryService, TradeExecutor tradeExecutor)
        {
            ActivityHistoryService = activityHistoryService;
            TradeExecutor = tradeExecutor;
        }

        public TradeOrderPersistenceProcessor CreateTradeOrderPersistenceProcessor()
        {
            return new TradeOrderPersistenceProcessor(ActivityHistoryService, TradeExecutor);
        }

        public WalletOperationPersistenceProcessor CreateWalletOperationPersistenceProcessor()
        {
            return new WalletOperationPersistenceProcessor(ActivityHistoryService);
        }
    }
}
