using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NBitcoin;
using Nethereum.Geth;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers.ETH
{
    public class EthereumProvider : AbstractProvider
    {
        public const string ETH = "ETH";

        public string ThisCoinSymbol { get; protected set; } = ETH;
        private const string Web3Url = "https://rinkeby.infura.io";
        private readonly ILogger<EthereumProvider> _logger;
        private readonly WalletOperationService _walletOperationService;
        private readonly EventHistoryService _eventHistoryService;
        private readonly RandomEntropyService _randomEntropyService;
        private readonly VersionControl _versionControl;
        private readonly Web3 _web3;
        private readonly decimal _withdrawalGasFee;
        private readonly IDictionary<string, decimal> _knownPublicKeyBalances = new Dictionary<string, decimal>();

        public EthereumProvider(
            ILogger<EthereumProvider> logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl
        ) : base(logger)
        {
            _logger = logger;
            _walletOperationService = walletOperationService;
            _eventHistoryService = eventHistoryService;
            _randomEntropyService = randomEntropyService;
            _versionControl = versionControl;
            _web3 = new Web3(Web3Url);
            _withdrawalGasFee = 20;
            if (GetType() == typeof(EthereumProvider))
            {
                // Do not implicitly call in (mocked) subclasses
                ProviderLookup[ThisCoinSymbol] = this;
            }
        }

        protected override async Task ListenForBlockchainEvents()
        {
            // Copy the collection as it's modified during iteration
            var keysIteration = new List<string>(_knownPublicKeyBalances.Keys);
            // Listen for deposits
            foreach (var publicKey in keysIteration)
            {
                decimal balance;
                try
                {
                    balance = GetBalance(publicKey).Result;
                }
                catch (Exception)
                {
                    _logger.LogError($"Could not receive blockchain balance of public key {publicKey}, " +
                                     $"service is probably offline, skipping the entire run");
                    return;
                }

                var oldBalance = _knownPublicKeyBalances[publicKey];
                if (balance > oldBalance)
                {
                    //var generateEvent = _eventHistoryService.FindWalletGenerateByPublicKey(publicKey);
                    var retry = false;
                    do
                    {
                        // Hot wallet does not change, so we won't request it multiple times
                        var depositHotWallet = _walletOperationService.GetHotWallet(publicKey, ThisCoinSymbol);
                        long lastTriedVersion = 0;
                        _versionControl.ExecuteUsingFixedVersion(currentVersion =>
                        {
                            if (retry && currentVersion == lastTriedVersion)
                            {
                                _logger.LogInformation(
                                    $"Blockchain deposit @ version number {currentVersion + 1} waiting for new events' integration...");
                                Task.Delay(1000).Wait();
                                return;
                            }

                            lastTriedVersion = currentVersion;

                            // We have acquired a lock, so the deposit event may have been already processed
                            balance = GetBalance(publicKey).Result;
                            oldBalance = _knownPublicKeyBalances[publicKey];
                            if (balance <= oldBalance)
                            {
                                _logger.LogInformation(
                                    "Blockchain deposit canceled, it was already processed by a newer event");
                                retry = false;
                                return;
                            }

                            _logger.LogInformation(
                                $"Detected {ThisCoinSymbol} blockchain deposit event @ public key {publicKey}, balance {oldBalance} => {balance}, {(retry ? "retrying" : "trying")} deposit event persistence @ version number {currentVersion + 1}");

                            var deposit = new WalletDepositEventEntry
                            {
                                User = depositHotWallet.User,
                                AccountId = depositHotWallet.AccountId,
                                CoinSymbol = ThisCoinSymbol,
                                DepositQty = balance - oldBalance,
                                NewBalance = balance,
                                LastWalletPublicKey = _walletOperationService.GetLastPublicKey(publicKey),
                                DepositWalletPublicKey = publicKey,
                                VersionNumber = currentVersion + 1
                            };
                            IList<EventEntry> persist = new List<EventEntry>
                            {
                                deposit
                            };
                            retry = null == _eventHistoryService.Persist(persist, currentVersion).Result;
                        });
                    }
                    while (retry);
                }
                else if (balance < oldBalance)
                {
                    _logger.LogInformation(
                        "Detected a negative value deposit event; assuming there is a withdrawal going on");
                    //throw new Exception(
                    //    "Deposit event caused balance to be reduced. This is really unexpected, maybe a deposit event wasn't synchronized properly?");
                }
            }
        }

        public override void ProcessEvent(WalletEventEntry eventEntry)
        {
            ProcessEvent((dynamic) eventEntry);
        }

        private void ProcessEvent(WalletGenerateEventEntry eventEntry)
        {
            _knownPublicKeyBalances.Add(eventEntry.LastWalletPublicKey, 0);
        }

        private void ProcessEvent(WalletWithdrawalEventEntry eventEntry)
        {
            var oldBalance = GetCurrentlyCachedBalance(eventEntry.LastWalletPublicKey).Result;
            var newBalance = oldBalance - eventEntry.WithdrawalQty;
            if (newBalance != eventEntry.NewBalance)
            {
                throw new Exception(
                    $"{typeof(EthereumProvider).Name} detected fatal event inconsistency of wallet values, " +
                    $"new balance event value of {eventEntry.NewBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.LastWalletPublicKey] = eventEntry.NewBalance;
        }

        private void ProcessEvent(WalletRevokeEventEntry eventEntry)
        {
            var revoke = (WalletEventEntry) _eventHistoryService.FindById(eventEntry.RevokeWalletEventEntryId);
            var oldBalance = _knownPublicKeyBalances[revoke.LastWalletPublicKey];
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

            _knownPublicKeyBalances[revoke.LastWalletPublicKey] = newBalance;
        }

        private void ProcessEvent(WalletDepositEventEntry eventEntry)
        {
            var oldBalance = _knownPublicKeyBalances[eventEntry.LastWalletPublicKey];
            var newBalance = oldBalance + eventEntry.DepositQty;
            if (newBalance != eventEntry.NewBalance)
            {
                throw new Exception(
                    $"{typeof(EthereumProvider).Name} detected fatal event inconsistency of wallet values, " +
                    $"new balance event value of {eventEntry.NewBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.LastWalletPublicKey] = eventEntry.NewBalance;
        }

        public override async Task<string> GenerateHdWallet()
        {
            //return "brass bus same payment express already energy direct type have venture afraid";
            const int bytesCount = 16;
            var resultEntropy = RandomUtils.GetBytes(bytesCount);
            var thirdPartyEntropy = _randomEntropyService.GetRandomBytes(bytesCount);
            //_logger.LogInformation( $"Combining two entropies:\n{new Mnemonic(Wordlist.English, resultEntropy)}\nand\n{new Mnemonic(Wordlist.English, thirdPartyEntropy)}");
            for (var i = 0; i < bytesCount; i++)
            {
                resultEntropy[i] = (byte) (resultEntropy[i] ^ thirdPartyEntropy[i]);
            }

            var seed = new Mnemonic(Wordlist.English, resultEntropy);
            //_logger.LogInformation($"Resulting entropy seed:\n{seed.ToString()}");
            return seed.ToString();
        }

        public override async Task<string> GetPublicKeyFromHdWallet(string hdSeed)
        {
            return new Wallet(hdSeed, "").GetAccount(0).Address;
        }

        public override async Task<bool> Withdraw(
            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal value)
        {
            if (GetCurrentlyCachedBalance(walletPublicKeyUserReference).Result < value)
            {
                Consolidate(walletPublicKeyUserReference, value, true);
            }

            var transaction = await new Web3(
                    new Account(
                        new Wallet(
                            _walletOperationService.GetHotWallet(walletPublicKeyUserReference, ThisCoinSymbol).HdSeed,
                            ""
                        ).GetAccount(0).PrivateKey
                    )
                ).Eth
                .GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(withdrawToPublicKey, value, _withdrawalGasFee);
            var success = transaction.HasErrors() ?? true;
            // The known balance structure will be reduced by the withdrawal quantity asynchronously
            return success;
        }

//        public override void OnWithdraw(string fromPublicKey, string toPublicKey, decimal value)
//        {
//            _knownPublicKeyBalances[fromPublicKey] -= value;
//        }

        public override void OnDeposit(string fromPublicKey, string toPublicKey, decimal value)
        {
            _knownPublicKeyBalances[toPublicKey] += value;
        }

        public override async Task<decimal> GetBalance(string publicKey)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(publicKey);
            return Web3.Convert.FromWei(balance.Value);
        }

        public override void Consolidate(string targetPublicKey, decimal targetBalance, bool allowMoreBalance)
        {
            _logger.LogInformation(
                $"Consolidation initiated for wallet {targetPublicKey}, target {targetBalance} {ThisCoinSymbol} {(allowMoreBalance ? "or more" : "")}");
            var currentBalance = GetCurrentlyCachedBalance(targetPublicKey).Result;
            var otherWallets = _walletOperationService.GetAllHotWallets(targetPublicKey, ThisCoinSymbol);
            // Don't try to move balance from the same wallet
            otherWallets.RemoveAll(wallet => wallet.PublicKey.Equals(targetPublicKey));
            // Sort descending from largest balance
            otherWallets.Sort(
                (a, b) =>
                    GetCurrentlyCachedBalance(b.PublicKey).Result
                        .CompareTo(GetCurrentlyCachedBalance(a.PublicKey).Result)
            );
            foreach (var hotWallet in otherWallets)
            {
                if (currentBalance >= targetBalance)
                {
                    return;
                }

                var balanceToWithdraw = GetCurrentlyCachedBalance(hotWallet.PublicKey).Result;
                if (!allowMoreBalance && currentBalance + balanceToWithdraw > targetBalance)
                {
                    balanceToWithdraw = targetBalance - currentBalance;
                }

                _logger.LogInformation(
                    $"Consolidation withdrawal of {balanceToWithdraw} {ThisCoinSymbol} from {hotWallet.PublicKey} to {targetPublicKey} initiated");
                // Mark target as already deposited, listener ignores such states
                _knownPublicKeyBalances[targetPublicKey] += balanceToWithdraw;
                // Withdraw and start listening for deposits on the other hot wallet
                Withdraw(hotWallet.PublicKey, targetPublicKey, balanceToWithdraw).Wait();
                _logger.LogInformation($"Consolidation withdrawal to {targetPublicKey} successful");
            }
        }

        public override async Task<decimal> GetCurrentlyCachedBalance(string publicKey)
        {
            // The zero is only returned if there was no Wallet Generate event processed yet,
            // which means that the command method, which called this, will surely be deemed invalid before it persists
            return _knownPublicKeyBalances.ContainsKey(publicKey) ? _knownPublicKeyBalances[publicKey] : 0m;
        }
    }
}
