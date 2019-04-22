using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Contracts;
using Nethereum.HdWallet;
using Nethereum.StandardTokenEIP20.ContractDefinition;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers
{
    public abstract class EthereumTokenProvider : SimpleProvider // where TokenTransferFunction:TransferFunction, new()
    {
        protected readonly long WithdrawalGasMultiplier = 21000;
        protected readonly decimal Gwei = 0.000000001m;
        protected readonly decimal _withdrawalGasPriceGwei;
        protected readonly string _contractAddress;
        protected readonly WalletOperationService _walletOperationService;
        protected readonly string Web3Url;
        private readonly Web3 _web3;

        public EthereumTokenProvider(
            string thisCoinSymbol,
            string contractAddress,
            ILogger logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration)
            : base(
                thisCoinSymbol,
                logger,
                walletOperationService,
                eventHistoryService,
                randomEntropyService,
                versionControl,
                configuration)
        {
            _contractAddress = contractAddress;
            _walletOperationService = walletOperationService;
            Web3Url = configuration["ETH:Web3Url"] ?? throw new ArgumentException("ETH:Web3Url");
            _web3 = new Web3(Web3Url);
            _withdrawalGasPriceGwei = decimal.Parse(
                configuration["ETH:WithdrawalGasPriceGwei"] ??
                throw new ArgumentException("ETH:WithdrawalGasPriceGwei"));
        }

        public override async Task<string> GetPublicKeyFromHdWallet(string hdSeed)
        {
            return new Wallet(hdSeed, "").GetAccount(0).Address;
        }

        public override async Task<string> GetPrivateKeyFromHdWallet(string hdSeed)
        {
            return new Wallet(hdSeed, "").GetAccount(0).PrivateKey;
        }

//        abstract protected  TokenTransferFunction WithdrawalFunction(
//            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee);

        public override async Task<decimal> GetBalance(string publicKey)
        {
            var tokenService = new Nethereum.StandardTokenEIP20.StandardTokenService(_web3, _contractAddress);
            var ownerBalanceTask = tokenService.BalanceOfQueryAsync(publicKey);
            return Web3.Convert.FromWei(await ownerBalanceTask);
        }

        public override decimal Fee()
        {
            return 0;
        }

        public decimal EthFee()
        {
            return _withdrawalGasPriceGwei * WithdrawalGasMultiplier * Gwei;
        }
    }
}
