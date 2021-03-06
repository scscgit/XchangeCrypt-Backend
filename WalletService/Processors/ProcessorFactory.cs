using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Processors.Command;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Processors
{
    public class ProcessorFactory
    {
        private readonly ILogger<WalletCommandProcessor> _processorLogger;
        private VersionControl VersionControl { get; }
        private EventHistoryService EventHistoryService { get; }
        private WalletOperationService WalletOperationService { get; }

        public ProcessorFactory(
            VersionControl versionControl,
            EventHistoryService eventHistoryService,
            WalletOperationService walletOperationService,
            ILogger<WalletCommandProcessor> processorLogger)
        {
            _processorLogger = processorLogger;
            VersionControl = versionControl;
            EventHistoryService = eventHistoryService;
            WalletOperationService = walletOperationService;
        }

        public WalletCommandProcessor CreateWalletOperationPersistenceProcessor()
        {
            return new WalletCommandProcessor(
                VersionControl, EventHistoryService, WalletOperationService, _processorLogger);
        }
    }
}
