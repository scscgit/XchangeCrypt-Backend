using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NBitcoin.SPV;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Providers;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Processors.Command
{
    public class WalletCommandProcessor
    {
        private readonly ILogger<WalletCommandProcessor> _logger;
        private VersionControl VersionControl { get; }
        private EventHistoryService EventHistoryService { get; }
        private WalletOperationService WalletOperationService { get; }

        /// <summary>
        /// Created via ProcessorFactory.
        /// </summary>
        public WalletCommandProcessor(
            VersionControl versionControl,
            EventHistoryService eventHistoryService,
            WalletOperationService walletOperationService,
            ILogger<WalletCommandProcessor> logger)
        {
            _logger = logger;
            VersionControl = versionControl;
            EventHistoryService = eventHistoryService;
            WalletOperationService = walletOperationService;
        }

        public async Task ExecuteWalletOperationCommand(
            string user, string accountId, string coinSymbol, string walletCommandType, decimal? amount,
            ObjectId? walletEventIdReference, string withdrawalTargetPublicKey, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            var retry = false;
            Action<IList<EventEntry>> afterPersistence = null;
            IList<EventEntry> eventEntries = null;
            IList<EventEntry> success;
            do
            {
                switch (walletCommandType)
                {
                    case MessagingConstants.WalletCommandTypes.Generate:
                        eventEntries = await PlanGenerateEvents(
                            user, accountId, coinSymbol, requestId, reportInvalidMessage, eventEntries);
                        break;

                    case MessagingConstants.WalletCommandTypes.Withdrawal:
                        eventEntries = await PlanWithdrawalEvents(
                            user, accountId, coinSymbol, withdrawalTargetPublicKey, amount.Value, requestId,
                            reportInvalidMessage, out afterPersistence, eventEntries);
                        break;

                    case MessagingConstants.WalletCommandTypes.RevokeDeposit:
                    case MessagingConstants.WalletCommandTypes.RevokeWithdrawal:
                        eventEntries = await PlanRevokeEvents(
                            user, accountId, coinSymbol, walletEventIdReference.Value, requestId, reportInvalidMessage,
                            eventEntries);
                        break;

                    default:
                        throw reportInvalidMessage($"Unrecognized wallet command type: {walletCommandType}");
                }

                _logger.LogInformation(
                    $"{(retry ? "Retrying" : "Trying")} to persist {eventEntries.Count.ToString()} event(s) planned by command requestId {requestId} on version number {eventEntries[0].VersionNumber.ToString()}");
                success = await EventHistoryService.Persist(eventEntries);
                retry = success == null;
                // Assuming the event listener has attempted to acquire the lock meanwhile
            }
            while (retry);

            _logger.LogInformation($"Successfully persisted events from requestId {requestId}");
            afterPersistence?.Invoke(success);
        }

        private async Task<IList<EventEntry>> PlanGenerateEvents(
            string user, string accountId, string coinSymbol, string requestId,
            Func<string, Exception> reportInvalidMessage, IList<EventEntry> previousEventEntries)
        {
            IList<EventEntry> plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                // Wallet generation is determined statically, so after private key gets persisted, we have the event
                if (previousEventEntries != null)
                {
                    foreach (var previousEventEntry in previousEventEntries)
                    {
                        previousEventEntry.VersionNumber = eventVersionNumber;
                        plannedEvents.Add(previousEventEntry);
                    }

                    return;
                }

                // Do the actual seed generation, storing it during a version control lock
                var hdSeed = AbstractProvider.ProviderLookup[coinSymbol].GenerateHdWallet().Result;
                var walletPublicKey = AbstractProvider
                    .ProviderLookup[coinSymbol].GetPublicKeyFromHdWallet(hdSeed).Result;
                _logger.LogInformation($"User {user} generated {coinSymbol} wallet with public key {walletPublicKey}");

                // TODO: based on specific cryptocurrency, we could omit wallet persistence and just increase HD index
                WalletOperationService.StoreHdWallet(
                    hdSeed, walletPublicKey, user, accountId, coinSymbol, eventVersionNumber);
                plannedEvents.Add(
                    new WalletGenerateEventEntry
                    {
                        VersionNumber = eventVersionNumber,
                        User = user,
                        AccountId = accountId,
                        CoinSymbol = coinSymbol,
                        NewSourcePublicKeyBalance = 0,
                        LastWalletPublicKey = walletPublicKey,
                    }
                );
            });
            return plannedEvents;
        }

        private Task<IList<EventEntry>> PlanWithdrawalEvents(
            string user, string accountId, string coinSymbol, string withdrawalTargetPublicKey, decimal amount,
            string requestId, Func<string, Exception> reportInvalidMessage,
            out Action<IList<EventEntry>> afterPersistence, IList<EventEntry> previousEventEntries)
        {
            // TODO: block concurrent withdrawals, as their cached balance is not stable (NewBalance becomes incorrect)
            IList<EventEntry> plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                var sourcePublicKey = WalletOperationService.GetLastPublicKey(user, accountId, coinSymbol);
                var sourcePublicKeyBalance = AbstractProvider.ProviderLookup[coinSymbol]
                    .GetCurrentlyCachedBalance(sourcePublicKey)
                    .Result;

                // The consolidation events will be first validated by TradingService
                if (sourcePublicKeyBalance < amount)
                {
                    var (consolidationEvents, expectedTargetBalance) = PlanConsolidateLocked(
                        user, accountId, sourcePublicKey, coinSymbol, amount, true, reportInvalidMessage,
                        currentVersionNumber).Result;
                    consolidationEvents.ForEach(plannedEvents.Add);
                    sourcePublicKeyBalance = expectedTargetBalance;
                }

                var withdrawalEventEntry = new WalletWithdrawalEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    LastWalletPublicKey = sourcePublicKey,
                    WithdrawalSourcePublicKey = sourcePublicKey,
                    // Note: the new public key balance can go to negative values, as there can be a consolidation
                    NewSourcePublicKeyBalance = sourcePublicKeyBalance - amount,
                    //BlockchainTransactionId = ,
                    WithdrawalTargetPublicKey = withdrawalTargetPublicKey,
                    WithdrawalQty = amount,
                    OverdrawnAndCanceledOrders = false,
                    Executed = false,
                    Validated = null,
                };
                plannedEvents.Add(withdrawalEventEntry);
            });
            // Prepares the withdrawal, or a compensating event, only after the event is successfully stored
            afterPersistence = eventEntries =>
            {
                var withdrawalEventEntry =
                    eventEntries.Single(eventEntry => eventEntry is WalletWithdrawalEventEntry)
                        as WalletWithdrawalEventEntry;
                // The withdrawal is asynchronous, we don't wait here
                var preparingAsync = AbstractProvider.ProviderLookup[coinSymbol].PrepareWithdrawalAsync(
                    withdrawalEventEntry, () =>
                    {
                        ExecuteWalletOperationCommand(user, accountId, coinSymbol,
                            MessagingConstants.WalletCommandTypes.RevokeWithdrawal, amount, withdrawalEventEntry.Id,
                            null,
                            requestId, silentErrors => null).Wait();
                    });
            };
            return Task.FromResult(plannedEvents);
        }

        private async Task<(List<EventEntry>, decimal)> PlanConsolidateLocked(
            string user, string accountId, string targetPublicKey, string coinSymbol, decimal targetBalance,
            bool allowMoreBalance, Func<string, Exception> reportInvalidMessage, long lockedCurrentVersionNumber)
        {
            var eventVersionNumber = lockedCurrentVersionNumber + 1;
            var plannedEvents = new List<EventEntry>();
            _logger.LogInformation(
                $"Consolidation calculation for wallet {targetPublicKey}, target {targetBalance} {coinSymbol} {(allowMoreBalance ? "or more" : "")}");
            var expectedCurrentBalance = await AbstractProvider.ProviderLookup[coinSymbol]
                .GetCurrentlyCachedBalance(targetPublicKey);

            if (targetBalance < expectedCurrentBalance)
            {
                // Direct consolidation command of reducing the target balance available to the user always
                // withdraws the remainder to the newest wallet
                var balanceReduction = expectedCurrentBalance - targetBalance;
                var destinationPublicKey = WalletOperationService.GetLastPublicKey(targetPublicKey);
                var destinationPreviousBalance = await AbstractProvider.ProviderLookup[coinSymbol]
                    .GetCurrentlyCachedBalance(destinationPublicKey);
                if (destinationPublicKey.Equals(targetPublicKey))
                {
                    throw reportInvalidMessage(
                        "Negative balance consolidation cannot be executed, because there was no newer wallet to receive the funds. Please generate a new wallet before cleaning up the old one");
                }

                _logger.LogInformation(
                    $"Consolidation withdrawal (balance reducing) of {balanceReduction} {coinSymbol} from {targetPublicKey} to {destinationPublicKey} planned");
                plannedEvents.Add(new WalletConsolidationTransferEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    // Not needed, we can fill this in later if ever needed, as it can be statically calculated
                    LastWalletPublicKey = null,
                    // Note: the new public key balance can go to negative values, as there can be a consolidation
                    NewSourcePublicKeyBalance = targetBalance,
                    NewTargetPublicKeyBalance = destinationPreviousBalance + balanceReduction,
                    TransferSourcePublicKey = targetPublicKey,
                    TransferTargetPublicKey = destinationPublicKey,
                    TransferQty = balanceReduction,
                    Executed = false,
                    Valid = null,
                });
                return (plannedEvents, targetBalance);
            }

            // Increasing the target wallet balance, there may be multiple required withdrawals
            var missingBalance = targetBalance - expectedCurrentBalance;
            var sourcePairs = await AbstractProvider.ProviderLookup[coinSymbol]
                .GetWalletsHavingSumBalance(missingBalance, targetPublicKey);

            foreach (var (sourcePublicKey, sourceBalance) in sourcePairs)
            {
                var balanceToWithdraw = sourceBalance;
                if (!allowMoreBalance && expectedCurrentBalance + balanceToWithdraw > targetBalance)
                {
                    balanceToWithdraw = targetBalance - expectedCurrentBalance;
                }

                expectedCurrentBalance += balanceToWithdraw;

                _logger.LogInformation(
                    $"Consolidation withdrawal of {balanceToWithdraw} {coinSymbol} from {sourcePublicKey} to {targetPublicKey} planned");
                plannedEvents.Add(new WalletConsolidationTransferEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    // Not needed, we can fill this in later if ever needed, as it can be statically calculated
                    LastWalletPublicKey = null,
                    // Note: the new public key balance can go to negative values, as there can be a consolidation
                    NewSourcePublicKeyBalance = sourceBalance - balanceToWithdraw,
                    NewTargetPublicKeyBalance = expectedCurrentBalance,
                    TransferSourcePublicKey = sourcePublicKey,
                    TransferTargetPublicKey = targetPublicKey,
                    TransferQty = balanceToWithdraw,
                    Executed = false,
                    Valid = null,
                });
            }

            if (allowMoreBalance && expectedCurrentBalance < targetBalance
                || !allowMoreBalance && expectedCurrentBalance != targetBalance)
            {
                throw reportInvalidMessage(
                    $"Consolidation planning self-check assertion failed, expected new balance {expectedCurrentBalance} of a target {coinSymbol} wallet {targetPublicKey} doesn't match requested value {targetBalance}");
            }

            return (plannedEvents, expectedCurrentBalance);
        }

        private Task<IList<EventEntry>> PlanRevokeEvents(
            string user, string accountId, string coinSymbol, ObjectId walletEventIdReference, string requestId,
            Func<string, Exception> reportInvalidMessage, IList<EventEntry> previousEventEntries)
        {
            // Administrator manipulation, or automatic withdrawal revocation
            IList<EventEntry> plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                var walletPublicKey = WalletOperationService.GetLastPublicKey(user, accountId, coinSymbol);
                var referencedEventEntry = EventHistoryService.FindById(walletEventIdReference);
                var balance = AbstractProvider.ProviderLookup[coinSymbol].GetCurrentlyCachedBalance(walletPublicKey)
                    .Result;
                if (referencedEventEntry is WalletDepositEventEntry deposit)
                {
                    balance -= deposit.DepositQty;
                }
                else if (referencedEventEntry is WalletWithdrawalEventEntry withdrawal)
                {
                    // If the withdrawal wasn't executed, we can still safely use the old value (+a-a=0)
                    if (withdrawal.Executed)
                    {
                        balance += withdrawal.WithdrawalQty;
                    }
                }
                else
                {
                    reportInvalidMessage($"Unsupported referenced event entry type {referencedEventEntry.GetType()}");
                }

                plannedEvents.Add(new WalletRevokeEventEntry
                {
                    User = user,
                    AccountId = accountId,
                    VersionNumber = eventVersionNumber,
                    CoinSymbol = coinSymbol,
                    LastWalletPublicKey = walletPublicKey,
                    NewSourcePublicKeyBalance = balance,
                    RevokeWalletEventEntryId = walletEventIdReference,
                });
            });
            return Task.FromResult(plannedEvents);
        }
    }
}
