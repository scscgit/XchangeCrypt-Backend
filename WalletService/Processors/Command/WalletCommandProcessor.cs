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
using XchangeCrypt.Backend.WalletService.Providers.ETH;
using XchangeCrypt.Backend.WalletService.Services;

namespace XchangeCrypt.Backend.WalletService.Processors.Command
{
    public class WalletCommandProcessor
    {
        private readonly ILogger _logger;
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
            ILogger logger)
        {
            _logger = logger;
            VersionControl = versionControl;
            EventHistoryService = eventHistoryService;
            WalletOperationService = walletOperationService;
        }

        public async Task RetryPersist(
            Func<IList<EventEntry>, (IList<EventEntry>, Action<IList<EventEntry>>)> eventPlanner)
        {
            bool retry;
            Action<IList<EventEntry>> afterPersistence;
            IList<EventEntry> eventEntries = null;
            IList<EventEntry> success;
            do
            {
                (eventEntries, afterPersistence) = eventPlanner(eventEntries);
                if (eventEntries == null)
                {
                    return;
                }

                success = await EventHistoryService.Persist(eventEntries);
                retry = success == null;
                // Assuming the event listener has attempted to acquire the lock meanwhile
            }
            while (retry);

            afterPersistence?.Invoke(success);
        }

        public async Task RetryPersist(Func<IList<EventEntry>, IList<EventEntry>> eventPlanner)
        {
            await RetryPersist(lastAttempt => (eventPlanner.Invoke(lastAttempt), null));
        }

        public async Task ExecuteWalletOperationCommand(
            string user, string accountId, string coinSymbol, string walletCommandType, decimal? amount,
            ObjectId? walletEventIdReference, string withdrawalTargetPublicKey, bool? firstGeneration, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            Action<IList<EventEntry>> afterPersistence = null;
            IList<EventEntry> eventEntries = null;
            await RetryPersist(lastAttempt =>
            {
                switch (walletCommandType)
                {
                    case MessagingConstants.WalletCommandTypes.Generate:
                        eventEntries = PlanGenerateEvents(
                            user, accountId, coinSymbol, firstGeneration.Value, requestId, reportInvalidMessage,
                            eventEntries
                        ).Result;
                        break;

                    case MessagingConstants.WalletCommandTypes.Withdrawal:
                        eventEntries = PlanWithdrawalEvents(
                            user, accountId, coinSymbol, withdrawalTargetPublicKey, amount.Value, requestId,
                            reportInvalidMessage, out afterPersistence, eventEntries
                        ).Result;
                        break;

                    case MessagingConstants.WalletCommandTypes.RevokeDeposit:
                    case MessagingConstants.WalletCommandTypes.RevokeWithdrawal:
                        eventEntries = PlanRevokeEvents(
                            user, accountId, coinSymbol, walletEventIdReference.Value, requestId,
                            reportInvalidMessage,
                            eventEntries
                        ).Result;
                        break;

                    default:
                        throw reportInvalidMessage($"Unrecognized wallet command type: {walletCommandType}");
                }

                _logger.LogInformation(
                    $"{(lastAttempt != null ? "Retrying" : "Trying")} to persist {eventEntries.Count.ToString()} event(s) planned by {walletCommandType} command requestId {requestId} on version number {eventEntries[0].VersionNumber.ToString()}");
                return (eventEntries, persistedEventEntries =>
                {
                    _logger.LogInformation($"Successfully persisted events from requestId {requestId}");
                    afterPersistence?.Invoke(persistedEventEntries);
                });
            });
        }

        private async Task<IList<EventEntry>> PlanGenerateEvents(
            string user, string accountId, string coinSymbol, bool firstGeneration, string requestId,
            Func<string, Exception> reportInvalidMessage, IList<EventEntry> previousEventEntries)
        {
            IList<EventEntry> plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                if (firstGeneration)
                {
                    var walletExists = false;
                    // Request identifier of an empty string means that we only want to generate a wallet if there is none,
                    // so that we don't spam the database too much (but nothing bad would happen either way)
                    try
                    {
                        walletExists = AbstractProvider.ProviderLookup[coinSymbol].GetCurrentlyCachedBalance(
                                           WalletOperationService.GetLastPublicKey(user, accountId, coinSymbol)
                                       ).Result != -1;
                    }
                    catch (Exception)
                    {
                        // There is no such wallet, so we can continue the generation
                    }

                    if (walletExists)
                    {
                        throw reportInvalidMessage($"The {coinSymbol} wallet was already generated");
                    }
                }

                var eventVersionNumber = currentVersionNumber + 1;
                // Wallet generation is determined statically, so after private key gets persisted, we have the event
                if (previousEventEntries != null)
                {
                    foreach (var previousEventEntry in previousEventEntries)
                    {
                        previousEventEntry.VersionNumber = eventVersionNumber;
                        plannedEvents.Add(previousEventEntry);
                        if (previousEventEntry is WalletGenerateEventEntry generate)
                        {
                            // Also update the creation version number
                            WalletOperationService.StoreHdWallet(
                                WalletOperationService
                                    .GetHotWallet(generate.LastWalletPublicKey, generate.CoinSymbol)
                                    .HdSeed,
                                generate.LastWalletPublicKey,
                                user,
                                accountId,
                                coinSymbol,
                                eventVersionNumber
                            );
                        }

                        return;
                    }
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
            string user, string accountId, string coinSymbol, string withdrawalTargetPublicKey, decimal amountInclFees,
            string requestId, Func<string, Exception> reportInvalidMessage,
            out Action<IList<EventEntry>> afterPersistence, IList<EventEntry> previousEventEntries)
        {
            if (amountInclFees < 0)
            {
                throw reportInvalidMessage(
                    "One does not simply withdraw negative amount. Hey, the client doesn't even let you do that");
            }

            // TODO: block concurrent withdrawals, as their cached balance is not stable (NewBalance becomes incorrect)
            IList<EventEntry> plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                var sourcePublicKey = WalletOperationService.GetLastPublicKey(user, accountId, coinSymbol);
                var sourcePublicKeyBalanceExclFees = AbstractProvider.ProviderLookup[coinSymbol]
                    .GetCurrentlyCachedBalance(sourcePublicKey)
                    .Result;
                if (sourcePublicKeyBalanceExclFees == -1)
                {
                    throw reportInvalidMessage(
                        "You probably haven't generated a wallet yet, since you were provided with an inactive public key");
                }

                var singleFee = AbstractProvider.ProviderLookup[coinSymbol].Fee();
                var combinedFee = singleFee;

                // If the wallet doesn't physically contain the coins, it'll be consolidated beforehand.
                // The consolidation events will be first validated by TradingService
                if (sourcePublicKeyBalanceExclFees < amountInclFees)
                {
                    var (consolidationEvents, expectedTargetBalanceExclFees) = PlanConsolidateLocked(
                        user, accountId, sourcePublicKey, coinSymbol, amountInclFees, true, reportInvalidMessage,
                        currentVersionNumber).Result;
                    consolidationEvents.ForEach(consolidationEvent =>
                    {
                        plannedEvents.Add(consolidationEvent);
                        combinedFee += consolidationEvent.TransferFee;
                    });
                    // This may be less than amount, considering there were fees paid
                    sourcePublicKeyBalanceExclFees = expectedTargetBalanceExclFees;
                }

                if (combinedFee >= amountInclFees)
                {
                    throw reportInvalidMessage(
                        $"The withdrawal fee ({combinedFee}{(plannedEvents.Count > 0 ? $" over {plannedEvents.Count} consolidations" : "")}) exceeds the withdrawal amount");
                }

                #region EthereumTokenProvider

                // Ethereum fee providing (a hotfix before we find a better solution)
                if (AbstractProvider.ProviderLookup[coinSymbol] is EthereumTokenProvider provider)
                {
                    if (AbstractProvider.ProviderLookup[EthereumProvider.ETH].GetBalance(sourcePublicKey).Result
                        < provider.EthFee())
                    {
                        // We pay several fees to be pretty sure we gain at least one
                        // TODO: implement an overload that requires target balance excluding fees
                        var randomMagicNumber = 2m;
                        // TODO: implement another ETH consolidation before a Token consolidation
                        var (consolidationEvents, expectedTargetBalanceExclFees) = PlanConsolidateLocked(
                            user, accountId, sourcePublicKey, EthereumProvider.ETH,
                            provider.EthFee() * randomMagicNumber,
                            false, reportInvalidMessage, currentVersionNumber).Result;
                        consolidationEvents.ForEach(plannedEvents.Add);
                    }
                }

                #endregion EthereumTokenProvider

                var withdrawalEventEntry = new WalletWithdrawalEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    LastWalletPublicKey = sourcePublicKey,
                    WithdrawalSourcePublicKey = sourcePublicKey,
                    // Withdrawal begins after consolidation, so its previous balance may start as a lower value
                    // As the amount includes consolidation fee, we only subtract the last withdrawal with one fee
                    NewSourcePublicKeyBalance =
                        sourcePublicKeyBalanceExclFees - (amountInclFees - combinedFee + singleFee),
                    //BlockchainTransactionId = ,
                    WithdrawalTargetPublicKey = withdrawalTargetPublicKey,
                    WithdrawalQty = amountInclFees - combinedFee,
                    WithdrawalSingleFee = singleFee,
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
                            MessagingConstants.WalletCommandTypes.RevokeWithdrawal, amountInclFees,
                            withdrawalEventEntry.Id, null, null, requestId, silentErrors => null
                        ).Wait();
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
            var expectedCurrentBalanceExclFees = await AbstractProvider.ProviderLookup[coinSymbol]
                .GetCurrentlyCachedBalance(targetPublicKey);
            var fee = AbstractProvider.ProviderLookup[coinSymbol].Fee();

            #region directConsolidation

            if (targetBalanceInclFees < expectedCurrentBalanceExclFees)
            {
                // Direct consolidation command of reducing the target balance available to the user always
                // withdraws the remainder to the newest wallet
                var balanceReduction = expectedCurrentBalanceExclFees - targetBalanceInclFees;
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
            var missingBalance = targetBalanceInclFees - expectedCurrentBalanceExclFees;
            var sourcePairs = await AbstractProvider.ProviderLookup[coinSymbol]
                .GetWalletsHavingSumBalance(missingBalance, targetPublicKey, false);

            foreach (var (sourcePublicKey, sourceBalance) in sourcePairs)
            {
                var balanceToWithdrawInclFee = sourceBalance;
                if (!allowMoreBalance &&
                    expectedCurrentBalanceExclFees + balanceToWithdrawInclFee > targetBalanceInclFees)
                {
                    balanceToWithdrawInclFee = targetBalanceInclFees - expectedCurrentBalanceExclFees;
                }

                expectedCurrentBalanceExclFees += balanceToWithdrawInclFee - fee;

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
                    NewTargetPublicKeyBalance = expectedCurrentBalanceExclFees,
                    TransferSourcePublicKey = sourcePublicKey,
                    TransferTargetPublicKey = targetPublicKey,
                    TransferQty = balanceToWithdrawInclFee - fee,
                    TransferFee = fee,
                    Executed = false,
                    Valid = null,
                });
            }

            if (allowMoreBalance
                && expectedCurrentBalanceExclFees + plannedEvents.Count * fee < targetBalanceInclFees
                || !allowMoreBalance
                && expectedCurrentBalanceExclFees + plannedEvents.Count * fee != targetBalanceInclFees)
            {
                throw reportInvalidMessage(
                    $"There are probably not enough collective funds in wallets. Consolidation planning self-check assertion failed, expected new balance {expectedCurrentBalanceExclFees} + fees {plannedEvents.Count * fee} of a target {coinSymbol} wallet {targetPublicKey} doesn't match requested target balance value {targetBalanceInclFees}{(allowMoreBalance ? "+" : "")}");
            }

            return (plannedEvents, expectedCurrentBalanceExclFees);
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
