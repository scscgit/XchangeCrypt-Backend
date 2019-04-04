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
                            try
                            {
                                balance = GetBalance(publicKey).Result;
                            }
                            catch (Exception)
                            {
                                _logger.LogError($"Could not receive blockchain balance of public key {publicKey}, " +
                                                 $"service is probably offline, skipping the single run");
                                retry = false;
                                return;
                            }

                            oldBalance = _knownPublicKeyBalances[publicKey];
                            if (balance <= oldBalance)
                            {
                                _logger.LogInformation(
                                    "Blockchain deposit canceled, it was already processed by a newer event");
                                retry = false;
                                return;
                            }

                            _logger.LogInformation(
                                $"{(retry ? "Retrying" : "Trying")} a detected {ThisCoinSymbol} blockchain deposit event @ public key {publicKey}, balance {oldBalance} => {balance}, event persistence @ version number {currentVersion + 1}");

                            var deposit = new WalletDepositEventEntry
                            {
                                User = depositHotWallet.User,
                                AccountId = depositHotWallet.AccountId,
                                CoinSymbol = ThisCoinSymbol,
                                DepositQty = balance - oldBalance,
                                NewSourcePublicKeyBalance = balance,
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
            if (newBalance != eventEntry.NewSourcePublicKeyBalance)
            {
                throw new Exception(
                    $"{GetType().Name} detected fatal event inconsistency of wallet values, " +
                    $"new balance event value of {eventEntry.NewSourcePublicKeyBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.LastWalletPublicKey] = eventEntry.NewSourcePublicKeyBalance;
        }

        private void ProcessEvent(WalletConsolidationTransferEventEntry eventEntry)
        {
            while (eventEntry.Valid == null)
            {
                _logger.LogError($"Consolidation {eventEntry.Id} is not validated by Trading Service yet, waiting...");
                Task.Delay(1000);
                eventEntry = (WalletConsolidationTransferEventEntry) _eventHistoryService.FindById(eventEntry.Id);
            }

            if (eventEntry.Valid == false)
            {
                return;
            }

            var oldBalanceSrc = GetCurrentlyCachedBalance(eventEntry.TransferSourcePublicKey).Result;
            var newBalanceSrc = oldBalanceSrc - eventEntry.TransferQty;
            var oldBalanceTarget = GetCurrentlyCachedBalance(eventEntry.TransferTargetPublicKey).Result;
            var newBalanceTarget = oldBalanceTarget + eventEntry.TransferQty;
            if (!eventEntry.Executed)
            {
                if (oldBalanceSrc == eventEntry.NewSourcePublicKeyBalance
                    && oldBalanceTarget == eventEntry.NewTargetPublicKeyBalance)
                {
                    // Already seems to be consolidated properly
                    _logger.LogError(
                        "Consolidation event already seems to have been successfully finished, even though it was unexpected. Marking the event as executed");
                }
                else if (newBalanceSrc == eventEntry.NewSourcePublicKeyBalance
                         && newBalanceTarget == eventEntry.NewTargetPublicKeyBalance)
                {
                    // Sanity check successful, we can execute the consolidation
                    // We start by expecting the deposit, not yielding it to user as his own deposit
                    _knownPublicKeyBalances[eventEntry.TransferTargetPublicKey] = newBalanceTarget;

                    var success = Withdraw(
                        eventEntry.TransferSourcePublicKey,
                        eventEntry.TransferTargetPublicKey,
                        eventEntry.TransferQty
                    ).Result;
                    if (!success)
                    {
                        throw new Exception(
                            $"Consolidation event id {eventEntry.Id} failed to execute the withdrawal. {GetType().Name} has to stop processing further events, requiring administrator to solve this issue");
                    }
                }
                else
                {
                    throw new Exception(
                        $"{GetType().Name} detected fatal event inconsistency of expected wallet balances during consolidation event id {eventEntry.Id} processing. Expecting source {oldBalanceSrc} -> {newBalanceSrc} to reach {eventEntry.NewSourcePublicKeyBalance}, target {oldBalanceTarget} -> {newBalanceTarget} to reach {eventEntry.NewTargetPublicKeyBalance}");
                }

                _eventHistoryService.ReportConsolidationExecuted(eventEntry);
            }
            // If the transfer was already executed, simply assert expected values and switch cached balances
            else
            {
                if (oldBalanceSrc != eventEntry.NewSourcePublicKeyBalance
                    || oldBalanceTarget != eventEntry.NewTargetPublicKeyBalance)
                {
                    throw new Exception(
                        $"{GetType().Name} detected fatal event inconsistency of expected wallet balances during consolidation event id {eventEntry.Id} processing. Expecting source {oldBalanceSrc} to be {eventEntry.NewSourcePublicKeyBalance} and reach {newBalanceSrc}, target {oldBalanceTarget} to be {eventEntry.NewTargetPublicKeyBalance} and reach {newBalanceTarget}");
                }

                _knownPublicKeyBalances[eventEntry.TransferTargetPublicKey] = newBalanceTarget;
            }

            // Unlock balance for deposit detection
            _knownPublicKeyBalances[eventEntry.TransferSourcePublicKey] = newBalanceSrc;
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
            if (newBalance != eventEntry.NewSourcePublicKeyBalance)
            {
                throw new Exception(
                    $"{GetType().Name} detected fatal event inconsistency of wallet values, " +
                    $"new balance event value of {eventEntry.NewSourcePublicKeyBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.LastWalletPublicKey] = eventEntry.NewSourcePublicKeyBalance;
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

        public override async Task PrepareWithdrawalAsync(
            WalletWithdrawalEventEntry withdrawalEventEntry, Action revocationAction)
        {
            // Saga operation: we wait for TradingService to confirm the withdrawal event
            var withdrawalDescription =
                $"Withdrawal of {withdrawalEventEntry.WithdrawalQty} {withdrawalEventEntry.CoinSymbol} of user {withdrawalEventEntry.User} to wallet {withdrawalEventEntry.WithdrawalTargetPublicKey}";
            for (var i = 0; i < 60; i++)
            {
                await Task.Delay(1000);
                var validated = ((WalletWithdrawalEventEntry)
                    _eventHistoryService.FindById(withdrawalEventEntry.Id)).Validated;
                switch (validated)
                {
                    case true:
                    {
                        var success = Withdraw(
                            withdrawalEventEntry.LastWalletPublicKey,
                            withdrawalEventEntry.WithdrawalTargetPublicKey,
                            withdrawalEventEntry.WithdrawalQty
                        ).Result;
                        _logger.LogInformation(
                            $"{withdrawalDescription} {(success ? "successful" : "has failed due to blockchain response, this is a critical error and the event will be revoked")}");
                        if (!success)
                        {
                            // Validation successful, yet the blockchain refused the transaction,
                            // so we can immediately unlock user's balance for trading or another retry
                            revocationAction();
                        }

                        return;
                    }
                    case false:
                        // Validation failed
                        _logger.LogInformation(
                            $"{withdrawalDescription} has failed due to negative response of a Trading Backend validation, revocation initiated");
                        revocationAction();
                        return;

                    case null:
                        _logger.LogInformation($"{withdrawalDescription} still waiting for a validation...");
                        break;
                }
            }

            // Timed out
            _logger.LogInformation($"{withdrawalDescription} has failed due to a timeout, revocation initiated");
            revocationAction();
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

        public override async Task<decimal> GetCurrentlyCachedBalance(string publicKey)
        {
            // The zero is only returned if there was no Wallet Generate event processed yet,
            // which means that the command method, which called this, will surely be deemed invalid before it persists
            return _knownPublicKeyBalances.ContainsKey(publicKey) ? _knownPublicKeyBalances[publicKey] : 0m;
        }
    }
}
