using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.BTC
{
    public class BitcoinProvider : BitcoinForkProvider
    {
        public const string BTC = "BTC";

        //private Node node;

        public BitcoinProvider(
            ILogger<BitcoinProvider> logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration)
            : base(
                BTC,
                0,
                Network.TestNet,
                logger,
                walletOperationService,
                eventHistoryService,
                randomEntropyService,
                versionControl,
                configuration)
        {
            if (GetType() == typeof(BitcoinProvider))
            {
                // Do not implicitly call in (mocked) subclasses
                ProviderLookup[ThisCoinSymbol] = this;
            }
        }
    }
}
