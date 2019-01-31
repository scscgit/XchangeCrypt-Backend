using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.TradingService.Processors.Command;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors
{
    public class ProcessorFactory
    {
        private readonly ILogger<TradeCommandProcessor> _tradeOrderPersistenceProcessorLogger;
        private readonly ILogger<WalletCommandProcessor> _walletOperationPersistenceProcessorLogger;
        private VersionControl VersionControl { get; }
        private TradingOrderService TradingOrderService { get; }
        private EventHistoryService EventHistoryService { get; }

        public ProcessorFactory(
            VersionControl versionControl,
            TradingOrderService tradingOrderService,
            EventHistoryService eventHistoryService,
            ILogger<TradeCommandProcessor> tradeOrderPersistenceProcessorLogger,
            ILogger<WalletCommandProcessor> walletOperationPersistenceProcessorLogger)
        {
            _tradeOrderPersistenceProcessorLogger = tradeOrderPersistenceProcessorLogger;
            _walletOperationPersistenceProcessorLogger = walletOperationPersistenceProcessorLogger;
            VersionControl = versionControl;
            TradingOrderService = tradingOrderService;
            EventHistoryService = eventHistoryService;
        }

        public TradeCommandProcessor CreateTradeOrderPersistenceProcessor()
        {
            return new TradeCommandProcessor(
                VersionControl, TradingOrderService, EventHistoryService, _tradeOrderPersistenceProcessorLogger);
        }

        public WalletCommandProcessor CreateWalletOperationPersistenceProcessor()
        {
            return new WalletCommandProcessor(EventHistoryService, _walletOperationPersistenceProcessorLogger);
        }
    }
}
