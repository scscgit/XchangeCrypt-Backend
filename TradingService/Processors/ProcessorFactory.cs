using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.TradingService.Processors.Command;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors
{
    public class ProcessorFactory
    {
        private readonly ILogger<TradeCommandProcessor> _tradeOrderPersistenceProcessorLogger;
        private VersionControl VersionControl { get; }
        private TradingOrderService TradingOrderService { get; }
        private UserService UserService { get; }
        private EventHistoryService EventHistoryService { get; }

        public ProcessorFactory(
            VersionControl versionControl,
            TradingOrderService tradingOrderService,
            UserService userService,
            EventHistoryService eventHistoryService,
            ILogger<TradeCommandProcessor> tradeOrderPersistenceProcessorLogger)
        {
            _tradeOrderPersistenceProcessorLogger = tradeOrderPersistenceProcessorLogger;
            VersionControl = versionControl;
            TradingOrderService = tradingOrderService;
            UserService = userService;
            EventHistoryService = eventHistoryService;
        }

        public TradeCommandProcessor CreateTradeOrderPersistenceProcessor()
        {
            return new TradeCommandProcessor(
                VersionControl, TradingOrderService, UserService, EventHistoryService,
                _tradeOrderPersistenceProcessorLogger);
        }
    }
}
