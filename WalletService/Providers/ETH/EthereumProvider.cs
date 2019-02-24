using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nethereum.Geth;
using Nethereum.HdWallet;
using Nethereum.Web3;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.ETH
{
    public class EthereumProvider : AbstractProvider
    {
        private readonly string thisCoinSymbol = "ETH";
        private readonly ILogger<EthereumProvider> _logger;
        private readonly WalletOperationService _walletOperationService;
        private readonly EventHistoryService _eventHistoryService;
        private readonly VersionControl _versionControl;
        private readonly Web3 _web3;
        private readonly decimal _withdrawalGasFee;
        private readonly IDictionary<string, decimal> _knownPublicKeyBalances = new Dictionary<string, decimal>();

        public EthereumProvider(
            ILogger<EthereumProvider> logger, WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService, VersionControl versionControl) : base(logger)
        {
            _logger = logger;
            _walletOperationService = walletOperationService;
            _eventHistoryService = eventHistoryService;
            _versionControl = versionControl;
            _web3 = new Web3("https://mainnet.infura.io");
            _withdrawalGasFee = 20;
            ProviderLookup["ETH"] = this;
        }

        protected override async Task ListenForBlockchainEvents()
        {
            // Listen for deposits
            foreach (var publicKey in _knownPublicKeyBalances.Keys)
            {
                var balance = await GetBalance(publicKey);
                var oldBalance = _knownPublicKeyBalances[publicKey];
                if (balance > oldBalance)
                {
                    _logger.LogInformation(
                        $"Detected blockchain deposit event @ public key {publicKey}, balance {oldBalance} => {balance}");
                    //var generateEvent = _eventHistoryService.FindWalletGenerateByPublicKey(publicKey);
                    IList<EventEntry> persist;
                    var retry = false;
                    do
                    {
                        if (retry)
                        {
                            _logger.LogInformation("Retrying deposit event persistence");
                        }

                        retry = true;
                        var now = DateTime.Now;
                        var currentVersion = _versionControl.CurrentVersion;
                        var deposit = new WalletDepositEventEntry
                        {
                            CoinSymbol = thisCoinSymbol,
                            DepositQty = balance - oldBalance,
                            EntryTime = now,
                            NewBalance = balance,
                            WalletPublicKey = publicKey,
                            VersionNumber = currentVersion
                        };
                        persist = new List<EventEntry>
                        {
                            deposit
                        };
                    }
                    while (null == await _eventHistoryService.Persist(persist));
                }
                else if (balance < oldBalance)
                {
                    throw new Exception(
                        "Deposit event caused balance to be reduced. This is really unexpected, maybe a deposit event wasn't synchronized properly?");
                }
            }
        }

        public override void ProcessEvent(WalletEventEntry eventEntry)
        {
            ProcessEvent((dynamic) eventEntry);
        }

        private void ProcessEvent(WalletGenerateEventEntry eventEntry)
        {
            _knownPublicKeyBalances[eventEntry.WalletPublicKey] = 0;
        }

        private void ProcessEvent(WalletWithdrawalEventEntry eventEntry)
        {
            var oldBalance = _knownPublicKeyBalances[eventEntry.WalletPublicKey];
            var newBalance = oldBalance - eventEntry.WithdrawalQty;
            if (newBalance != eventEntry.NewBalance)
            {
                throw new Exception(
                    $"{typeof(EthereumProvider).Name} detected fatal event inconsistency of wallet values, " +
                    $"new balance event value of {eventEntry.NewBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.WalletPublicKey] = eventEntry.NewBalance;
        }

        private void ProcessEvent(WalletRevokeEventEntry eventEntry)
        {
            var revoke = (WalletEventEntry) _eventHistoryService.FindById(eventEntry.RevokeWalletEventEntryId);
            var oldBalance = _knownPublicKeyBalances[revoke.WalletPublicKey];
            decimal newBalance;
            switch (revoke)
            {
                case WalletDepositEventEntry deposit:
                    newBalance = oldBalance - deposit.DepositQty;
                    break;
                case WalletWithdrawalEventEntry withdrawal:
                    newBalance = oldBalance + withdrawal.WithdrawalQty;
                    break;
                default:
                    throw new Exception("Unsupported wallet revoke event type " + revoke.GetType().Name);
            }

            _knownPublicKeyBalances[revoke.WalletPublicKey] = newBalance;
        }

        private void ProcessEvent(WalletDepositEventEntry eventEntry)
        {
            var oldBalance = _knownPublicKeyBalances[eventEntry.WalletPublicKey];
            var newBalance = oldBalance + eventEntry.DepositQty;
            if (newBalance != eventEntry.NewBalance)
            {
                throw new Exception(
                    $"{typeof(EthereumProvider).Name} detected fatal event inconsistency of wallet values, " +
                    $"new balance event value of {eventEntry.NewBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.WalletPublicKey] = eventEntry.NewBalance;
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
