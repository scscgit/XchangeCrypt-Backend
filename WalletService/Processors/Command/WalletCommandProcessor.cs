using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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

                var combinedFee = AbstractProvider.ProviderLookup[coinSymbol].Fee();

                // If the wallet doesn't physically contain the coins, it'll be consolidated beforehand.
                // The consolidation events will be first validated by TradingService
                if (sourcePublicKeyBalance < amount)
                {
                    var (consolidationEvents, expectedTargetBalance) = PlanConsolidateLocked(
                        user, accountId, sourcePublicKey, coinSymbol, amount, true, reportInvalidMessage,
                        currentVersionNumber).Result;
                    consolidationEvents.ForEach(consolidationEvent =>
                    {
                        plannedEvents.Add(consolidationEvent);
                        combinedFee += consolidationEvent.TransferFee;
                    });
                    // This may be less than amount, considering there were fees paid
                    sourcePublicKeyBalance = expectedTargetBalance;
                }

                if (combinedFee >= amount)
                {
                    throw reportInvalidMessage(
                        $"The withdrawal fee ({combinedFee}{(plannedEvents.Count > 0 ? $" over {plannedEvents.Count} consolidations" : "")}) exceeds the withdrawal amount");
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
                    NewSourcePublicKeyBalance = sourcePublicKeyBalance - amount - combinedFee,
                    //BlockchainTransactionId = ,
                    WithdrawalTargetPublicKey = withdrawalTargetPublicKey,
                    WithdrawalQty = amount - combinedFee,
                    WithdrawalCombinedFee = combinedFee,
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

        private async Task<(List<WalletConsolidationTransferEventEntry>, decimal)> PlanConsolidateLocked(
            string user, string accountId, string targetPublicKey, string coinSymbol, decimal targetBalanceInclFees,
            bool allowMoreBalance, Func<string, Exception> reportInvalidMessage, long lockedCurrentVersionNumber)
        {
            var eventVersionNumber = lockedCurrentVersionNumber + 1;
            var plannedEvents = new List<WalletConsolidationTransferEventEntry>();
            _logger.LogInformation(
                $"Consolidation calculation for wallet {targetPublicKey}, target {targetBalanceInclFees} {coinSymbol} {(allowMoreBalance ? "or more" : "")}");
            var expectedCurrentBalance = await AbstractProvider.ProviderLookup[coinSymbol]
                .GetCurrentlyCachedBalance(targetPublicKey);
            var fee = AbstractProvider.ProviderLookup[coinSymbol].Fee();

            #region directConsolidation

            if (targetBalanceInclFees < expectedCurrentBalance)
            {
                // Direct consolidation command of reducing the target balance available to the user always
                // withdraws the remainder to the newest wallet
                var balanceReduction = expectedCurrentBalance - targetBalanceInclFees;
                var destinationPublicKey = WalletOperationService.GetLastPublicKey(targetPublicKey);
                var destinationPreviousBalance = await AbstractProvider.ProviderLookup[coinSymbol]
                    .GetCurrentlyCachedBalance(destinationPublicKey);
                if (destinationPublicKey.Equals(targetPublicKey))
                {
                    throw reportInvalidMessage(
                        "Negative balance consolidation cannot be executed, because there was no newer wallet to receive the funds. Please generate a new wallet before cleaning up the old one");
                }

                _logger.LogInformation(
                    $"Consolidation withdrawal (balance reducing) of {balanceReduction} - {fee} {coinSymbol} from {targetPublicKey} to {destinationPublicKey} planned");
                plannedEvents.Add(new WalletConsolidationTransferEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    // Not needed, we can fill this in later if ever needed, as it can be statically calculated
                    LastWalletPublicKey = null,
                    // Note: the new public key balance can go to negative values, as there can be a consolidation
                    NewSourcePublicKeyBalance = targetBalanceInclFees,
                    NewTargetPublicKeyBalance = destinationPreviousBalance + balanceReduction - fee,
                    TransferSourcePublicKey = targetPublicKey,
                    TransferTargetPublicKey = destinationPublicKey,
                    TransferQty = balanceReduction - fee,
                    TransferFee = fee,
                    Executed = false,
                    Valid = null,
                });
                return (plannedEvents, targetBalanceInclFees);
            }

            #endregion directConsolidation

            // Increasing the target wallet balance, there may be multiple required withdrawals
            var missingBalance = targetBalanceInclFees - expectedCurrentBalance;
            var sourcePairs = await AbstractProvider.ProviderLookup[coinSymbol]
                .GetWalletsHavingSumBalance(missingBalance, targetPublicKey, false);

            foreach (var (sourcePublicKey, sourceBalance) in sourcePairs)
            {
                var balanceToWithdrawInclFee = sourceBalance;
                if (!allowMoreBalance && expectedCurrentBalance + balanceToWithdrawInclFee > targetBalanceInclFees)
                {
                    balanceToWithdrawInclFee = targetBalanceInclFees - expectedCurrentBalance;
                }

                expectedCurrentBalance += balanceToWithdrawInclFee - fee;

                _logger.LogInformation(
                    $"Consolidation withdrawal of {balanceToWithdrawInclFee} - {fee} {coinSymbol} from {sourcePublicKey} to {targetPublicKey} planned");
                plannedEvents.Add(new WalletConsolidationTransferEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    // Not needed, we can fill this in later if ever needed, as it can be statically calculated
                    LastWalletPublicKey = null,
                    // Note: the new public key balance can go to negative values, as there can be a consolidation
                    NewSourcePublicKeyBalance = sourceBalance - balanceToWithdrawInclFee,
                    NewTargetPublicKeyBalance = expectedCurrentBalance,
                    TransferSourcePublicKey = sourcePublicKey,
                    TransferTargetPublicKey = targetPublicKey,
                    TransferQty = balanceToWithdrawInclFee - fee,
                    TransferFee = fee,
                    Executed = false,
                    Valid = null,
                });
            }

            if (allowMoreBalance && expectedCurrentBalance + plannedEvents.Count * fee < targetBalanceInclFees
                || !allowMoreBalance && expectedCurrentBalance + plannedEvents.Count * fee != targetBalanceInclFees)
            {
                throw reportInvalidMessage(
                    $"Consolidation planning self-check assertion failed, expected new balance {expectedCurrentBalance} + fees {plannedEvents.Count * fee} of a target {coinSymbol} wallet {targetPublicKey} doesn't match requested target balance value {targetBalanceInclFees}{(allowMoreBalance ? "+" : "")}");
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
