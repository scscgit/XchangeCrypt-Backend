using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Providers
{
    public abstract class SimpleProvider : AbstractProvider
    {
        public string ThisCoinSymbol { get; protected set; }

        private readonly WalletOperationService _walletOperationService;
        private readonly EventHistoryService _eventHistoryService;
        private readonly RandomEntropyService _randomEntropyService;
        private readonly VersionControl _versionControl;
        private readonly IDictionary<string, decimal> _knownPublicKeyBalances = new Dictionary<string, decimal>();

        protected SimpleProvider(
            string thisCoinSymbol,
            ILogger logger,
            WalletOperationService walletOperationService,
            EventHistoryService eventHistoryService,
            RandomEntropyService randomEntropyService,
            VersionControl versionControl,
            IConfiguration configuration
        ) : base(logger)
        {
            ThisCoinSymbol = thisCoinSymbol;
            _walletOperationService = walletOperationService;
            _eventHistoryService = eventHistoryService;
            _randomEntropyService = randomEntropyService;
            _versionControl = versionControl;
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
                                     $"service is probably offline, skipping the wallet and waiting a few seconds");
                    Task.Delay(2000).Wait();
                    continue;
                }

                var oldBalance = _knownPublicKeyBalances[publicKey];
                if (balance == oldBalance)
                {
                    continue;
                }

                if (balance <= oldBalance)
                {
                    if (balance < oldBalance)
                    {
                        _logger.LogInformation(
                            "Detected a negative value deposit event; assuming there is a withdrawal going on");
                        //throw new Exception(
                        //    "Deposit event caused balance to be reduced. This is really unexpected, maybe a deposit event wasn't synchronized properly?");
                    }

                    continue;
                }

                // Generate event does not change, so we won't request it multiple times
                var generateEvent = _eventHistoryService.FindWalletGenerateByPublicKey(publicKey);
                var retry = false;
                do
                {
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

                        // We have acquired a lock, so the deposit event may have been already processed
                        try
                        {
                            balance = GetBalance(publicKey).Result;
                        }
                        catch (Exception)
                        {
                            _logger.LogError($"Could not receive blockchain balance of public key {publicKey}, " +
                                             $"service is probably offline, waiting a few seconds");
                            Task.Delay(5000).Wait();
                            retry = true;
                            return;
                        }

                        lastTriedVersion = currentVersion;

                        oldBalance = GetCurrentlyCachedBalance(publicKey).Result;
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
                            User = generateEvent.User,
                            AccountId = generateEvent.AccountId,
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

                _logger.LogInformation(
                    $"{ThisCoinSymbol} blockchain deposit event successfully persisted @ public key {publicKey}, balance {oldBalance} => {balance}");
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
            // We cannot reduce our cached balance, unlocking it for detection as a deposit,
            // until we are sure the withdrawal is valid and it has been executed in a previous service run
            while (eventEntry.Validated == null)
            {
                _logger.LogWarning(
                    $"Withdrawal event {eventEntry.Id} is not validated by Trading Service yet, waiting...");
                Task.Delay(1000).Wait();
                eventEntry = (WalletWithdrawalEventEntry) _eventHistoryService.FindById(eventEntry.Id);
            }

            if (eventEntry.Validated == false || eventEntry.Executed == false)
            {
                return;
            }

            // We only process the balance unlock if it hasn't been executed in this service run
            OnWithdrawal(eventEntry);
        }

        private void OnWithdrawal(WalletWithdrawalEventEntry eventEntry)
        {
            var oldBalance = GetCurrentlyCachedBalance(eventEntry.WithdrawalSourcePublicKey).Result;
            var newBalance = oldBalance - eventEntry.WithdrawalQty;
            if (newBalance != eventEntry.NewSourcePublicKeyBalance)
            {
                throw new Exception(
                    $"{GetType().Name} withdrawal event processing detected fatal event inconsistency of wallet values, new balance event value of {eventEntry.NewSourcePublicKeyBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.LastWalletPublicKey] = eventEntry.NewSourcePublicKeyBalance;
        }

        private void ProcessEvent(WalletConsolidationTransferEventEntry eventEntry)
        {
            while (eventEntry.Valid == null)
            {
                _logger.LogWarning(
                    $"Consolidation event {eventEntry.Id} is not validated by Trading Service yet, waiting...");
                Task.Delay(1000).Wait();
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
            decimal oldBalance;
            decimal newBalance;
            string revokedPublicKey;
            switch (revoke)
            {
                case WalletDepositEventEntry deposit:
                    revokedPublicKey = deposit.DepositWalletPublicKey;
                    oldBalance = _knownPublicKeyBalances[revokedPublicKey];
                    newBalance = oldBalance - deposit.DepositQty;
                    break;
                case WalletWithdrawalEventEntry withdrawal:
                    if (withdrawal.Validated == false)
                    {
                        // Not revoking event that was not executed
                        return;
                    }

                    revokedPublicKey = withdrawal.WithdrawalSourcePublicKey;
                    oldBalance = _knownPublicKeyBalances[revokedPublicKey];
                    newBalance = oldBalance + withdrawal.WithdrawalQty;
                    break;
                default:
                    throw new Exception("Unsupported wallet revoke event type " + revoke.GetType().Name);
            }

            _knownPublicKeyBalances[revokedPublicKey] = newBalance;
        }

        private void ProcessEvent(WalletDepositEventEntry eventEntry)
        {
            var oldBalance = GetCurrentlyCachedBalance(eventEntry.DepositWalletPublicKey).Result;
            var newBalance = oldBalance + eventEntry.DepositQty;
            if (newBalance != eventEntry.NewSourcePublicKeyBalance)
            {
                throw new Exception(
                    $"{GetType().Name} deposit event processing detected fatal event inconsistency of wallet values, new balance event value of {eventEntry.NewSourcePublicKeyBalance} should be {newBalance}");
            }

            _knownPublicKeyBalances[eventEntry.DepositWalletPublicKey] = eventEntry.NewSourcePublicKeyBalance;
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

        public override async Task PrepareWithdrawalAsync(
            WalletWithdrawalEventEntry withdrawalEventEntry, Action revocationAction)
        {
            var withdrawalDescription =
                $"Withdrawal of {withdrawalEventEntry.WithdrawalQty} {withdrawalEventEntry.CoinSymbol} of user {withdrawalEventEntry.User} to wallet {withdrawalEventEntry.WithdrawalTargetPublicKey}";

            // Saga operation: we wait for TradingService to confirm the withdrawal event
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
                        if (success)
                        {
                            // Note: this must occur after event processing by this own service
                            _eventHistoryService.ReportWithdrawalExecuted(withdrawalEventEntry);
                            // We just avoided re-processing the withdrawal event, so we have to fire it manually
                            OnWithdrawal(withdrawalEventEntry);
                        }
                        else
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

        public override async Task<decimal> GetCurrentlyCachedBalance(string publicKey)
        {
            // The zero is only returned if there was no Wallet Generate event processed yet,
            // which means that the command method, which called this, will surely be deemed invalid before it persists
            return _knownPublicKeyBalances.ContainsKey(publicKey) ? _knownPublicKeyBalances[publicKey] : 0m;
        }

        public override async Task<List<(string, decimal)>> GetWalletsHavingSumBalance(
            decimal sumBalance, string excludePublicKey)
        {
            var sortedPairs = _knownPublicKeyBalances
                // Take the list, only considering non-zero balances and excluding target balance
                .Where(pair => pair.Value > 0 && !pair.Key.Equals(excludePublicKey))
                // Take the smallest possible wallets larger than sumBalance first, otherwise sort from largest
                .GroupBy(pair => pair.Value >= sumBalance)
                .SelectMany(
                    group =>
                    {
                        var list = group.ToList();
                        if (group.Key)
                        {
                            list.Sort((a, b) => b.Value.CompareTo(a.Value));
                        }
                        else
                        {
                            list.Sort((a, b) => a.Value.CompareTo(b.Value));
                        }

                        return list;
                    }
                ).ToList();

            var result = new List<(string, decimal)>();
            foreach (var (key, value) in sortedPairs)
            {
                result.Add((key, value));
                sumBalance -= value;
                if (sumBalance <= 0)
                {
                    // Already exceeded the sum
                    break;
                }
            }

            return result;
        }
    }
}
