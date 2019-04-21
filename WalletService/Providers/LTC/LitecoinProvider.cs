using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Providers.ETH;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.LTC
{
    public class LitecoinProvider : BitcoinForkProvider
    {
        public const string LTC = "LTC";

        public LitecoinProvider(
            ILogger<LitecoinProvider> logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration)
            : base(
                LTC,
                2,
                NBitcoin.Altcoins.Litecoin.Instance.Testnet,
                logger,
                walletOperationService,
                eventHistoryService,
                randomEntropyService,
                versionControl,
                configuration)
        {
            node = Node.Connect(net);
            node.VersionHandshake();
            var chain = node.GetChain();
            // TODO: process new blocks since last checkpoint - the Node.Connect doesn't work as it should, use local

            if (GetType() == typeof(EthereumProvider))
            {
                // Do not implicitly call in (mocked) subclasses
                ProviderLookup[ThisCoinSymbol] = this;
            }
        }

        public override async Task<decimal> GetBalance(string publicKey)
        {
            return await GetCurrentlyCachedBalance(publicKey);
        }
    }
}
