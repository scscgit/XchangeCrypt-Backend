using System;
using System.Collections.Generic;
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
            Action afterPersistence = null;
            IList<EventEntry> eventEntries = null;
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
                var success = await EventHistoryService.Persist(eventEntries);
                retry = success == null;
                // Assuming the event listener has attempted to acquire the lock meanwhile
            }
            while (retry);

            _logger.LogInformation($"Successfully persisted events from requestId {requestId}");
            afterPersistence?.Invoke();
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
                        NewBalance = 0,
                        LastWalletPublicKey = walletPublicKey,
                    }
                );
            });
            return plannedEvents;
        }

        private Task<IList<EventEntry>> PlanWithdrawalEvents(
            string user, string accountId, string coinSymbol, string withdrawalTargetPublicKey, decimal amount,
            string requestId, Func<string, Exception> reportInvalidMessage, out Action afterPersistence,
            IList<EventEntry> previousEventEntries)
        {
            WalletWithdrawalEventEntry withdrawalEventEntry = null;
            string walletPublicKey = null;
            IList<EventEntry> plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                walletPublicKey = WalletOperationService.GetLastPublicKey(user, accountId, coinSymbol);
                var balance = AbstractProvider.ProviderLookup[coinSymbol].GetCurrentlyCachedBalance(walletPublicKey)
                    .Result;
                withdrawalEventEntry = new WalletWithdrawalEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    LastWalletPublicKey = walletPublicKey,
                    NewBalance = balance - amount,
                    //BlockchainTransactionId = ,
                    WithdrawalTargetPublicKey = withdrawalTargetPublicKey,
                    WithdrawalQty = amount,
                };
                plannedEvents.Add(withdrawalEventEntry);
            });
            // Executes the withdrawal, or a compensating event, only after the event is successfully stored
            afterPersistence = () =>
            {
                var success = AbstractProvider.ProviderLookup[coinSymbol]
                    .Withdraw(walletPublicKey, withdrawalTargetPublicKey, amount).Result;
                _logger.LogInformation(
                    $"Withdrawal of {amount} {coinSymbol} of user {user} to wallet {withdrawalTargetPublicKey} {(success ? "successful" : "has failed, this is a critical error and the event will be revoked")}");
                if (!success)
                {
                    ExecuteWalletOperationCommand(user, accountId, coinSymbol,
                        MessagingConstants.WalletCommandTypes.RevokeWithdrawal, amount, withdrawalEventEntry.Id, null,
                        requestId, reportInvalidMessage).Wait();
                    reportInvalidMessage(
                        $"Couldn't withdraw {amount} {coinSymbol} of user {user} to wallet {withdrawalTargetPublicKey}");
                }
            };
            return Task.FromResult(plannedEvents);
        }

        private Task<IList<EventEntry>> PlanRevokeEvents(
            string user, string accountId, string coinSymbol, ObjectId walletEventIdReference, string requestId,
            Func<string, Exception> reportInvalidMessage, IList<EventEntry> previousEventEntries)
        {
            // Administrator only
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
                    balance += withdrawal.WithdrawalQty;
                }
                else
                {
                    reportInvalidMessage($"Unsupported referenced event entry type {referencedEventEntry.GetType()}");
                }

                plannedEvents.Add(new WalletRevokeEventEntry
                {
                    VersionNumber = eventVersionNumber,
                    CoinSymbol = coinSymbol,
                    LastWalletPublicKey = walletPublicKey,
                    NewBalance = balance,
                });
            });
            return Task.FromResult(plannedEvents);
        }
    }
}
