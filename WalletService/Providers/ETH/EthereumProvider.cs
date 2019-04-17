using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.ETH
{
    public class EthereumProvider : SimpleProvider
    {
        public const string ETH = "ETH";

        private readonly string Web3Url;
        private readonly Web3 _web3;
        private readonly decimal _withdrawalGasFee;
        private readonly WalletOperationService _walletOperationService;
        private readonly EventHistoryService _eventHistoryService;
        private readonly VersionControl _versionControl;

        public EthereumProvider(
            ILogger<EthereumProvider> logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration
        ) : base(
            ETH,
            logger,
            walletOperationService,
            eventHistoryService,
            randomEntropyService,
            versionControl,
            configuration)
        {
            _walletOperationService = walletOperationService;
            _eventHistoryService = eventHistoryService;
            _versionControl = versionControl;
            Web3Url = configuration["ETH:Web3Url"] ?? throw new ArgumentException("ETH:Web3Url");
            _web3 = new Web3(Web3Url);
            _withdrawalGasFee = decimal.Parse(
                configuration["ETH:WithdrawalGasFee"] ?? throw new ArgumentException("ETH:WithdrawalGasFee"));
            if (GetType() == typeof(EthereumProvider))
            {
                // Do not implicitly call in (mocked) subclasses
                ProviderLookup[ThisCoinSymbol] = this;
            }
        }

        public override async Task<decimal> GetBalance(string publicKey)
        {
            // Adding a limit of 4 requests per second, TODO switch to a local fast node
            Task.Delay(250).Wait();
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(publicKey);
            return Web3.Convert.FromWei(balance.Value);
        }

        public override async Task<string> GetPublicKeyFromHdWallet(string hdSeed)
        {
            return new Wallet(hdSeed, "").GetAccount(0).Address;
        }

        public override async Task<bool> Withdraw(
            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal value)
        {
            var transaction = await new Web3(
                    new Account(
                        new Wallet(
                            _walletOperationService.GetHotWallet(walletPublicKeyUserReference, ThisCoinSymbol).HdSeed,
                            ""
                        ).GetAccount(0).PrivateKey
                    ),
                    Web3Url
                ).Eth
                .GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(withdrawToPublicKey, value, _withdrawalGasFee);
            var success = !(transaction.HasErrors() ?? true);
            // The known balance structure will be reduced by the withdrawal quantity asynchronously
            return success;
        }
    }
}
