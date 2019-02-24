using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nethereum.Geth;
using Nethereum.HdWallet;
using Nethereum.Web3;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.ETH
{
    public class EthereumProvider : AbstractProvider
    {
        private readonly ILogger<EthereumProvider> _logger;
        private readonly WalletOperationService _walletOperationService;
        private readonly Web3 _web3;
        private readonly decimal _withdrawalGasFee;
        private readonly IList<string> _knownPublicKeys = new List<string>();

        public EthereumProvider(ILogger<EthereumProvider> logger, WalletOperationService walletOperationService) :
            base(logger)
        {
            _logger = logger;
            _walletOperationService = walletOperationService;
            _web3 = new Web3("https://mainnet.infura.io");
            _withdrawalGasFee = 20;
            ProviderLookup["ETH"] = this;
        }

        protected override async Task ListenForEvents()
        {
            throw new NotImplementedException();
        }

        protected override void ProcessEvent(WalletEventEntry eventEntry)
        {
            ProcessEvent((dynamic) eventEntry);
        }

        private void ProcessEvent(WalletWithdrawalEventEntry eventEntry)
        {
        }

        private void ProcessEvent(WalletRevokeEventEntry eventEntry)
        {
        }

        private void ProcessEvent(WalletDepositEventEntry eventEntry)
        {
        }


        public override async Task<string> GenerateHdWallet()
        {
            //TODO
            return "brass bus same payment express already energy direct type have venture afraid";
        }

        public override async Task<string> GetPublicKeyFromHdWallet(string hdSeed)
        {
            return new Wallet(hdSeed, null).GetAccount(0).Address;
        }

        public override async Task<bool> Withdraw(string withdrawToPublicKey, decimal value)
        {
            var transaction = await _web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(withdrawToPublicKey, value, _withdrawalGasFee);
            return transaction.HasErrors() ?? true;
        }

        public override async Task OnDeposit(string fromPublicKey, string toPublicKey, decimal value)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<decimal> GetBalance(string publicKey)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(publicKey);
            return Web3.Convert.FromWei(balance.Value);
        }
    }
}
