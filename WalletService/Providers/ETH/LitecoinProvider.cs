using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.ETH
{
    public class LitecoinProvider : EthereumProvider
    {
        public const string LTC = "LTC";

        public LitecoinProvider(ILogger<LitecoinProvider> logger, WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService, RandomEntropyService randomEntropyService,
            VersionControl versionControl, IConfiguration configuration) : base(logger,
            walletOperationService, eventHistoryService, randomEntropyService, versionControl, configuration)
        {
            ThisCoinSymbol = LTC;
            // TODO: DEVELOPMENT ONLY, replace by actual provider!
            AbstractProvider.ProviderLookup[ThisCoinSymbol] = this;
        }
    }
}
