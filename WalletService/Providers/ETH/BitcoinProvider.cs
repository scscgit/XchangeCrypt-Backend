using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.ETH
{
    public class BitcoinProvider : EthereumProvider
    {
        public const string BTC = "BTC";

        public BitcoinProvider(ILogger<EthereumProvider> logger, WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService, RandomEntropyService randomEntropyService,
            VersionControl versionControl) : base(logger,
            walletOperationService, eventHistoryService, randomEntropyService, versionControl)
        {
            ThisCoinSymbol = BTC;
            // TODO: DEVELOPMENT ONLY, replace by actual provider!
            AbstractProvider.ProviderLookup[ThisCoinSymbol] = this;
        }
    }
}
