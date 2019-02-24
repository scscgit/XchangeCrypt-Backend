using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            string user, string accountId, string coinSymbol, string walletCommandType, decimal amount,
            string walletEventIdReference, string requestId, Func<string, Exception> reportInvalidMessage)
        {
            var retry = false;
            do
            {
                IList<EventEntry> eventEntries;
                switch (walletCommandType)
                {
                    case MessagingConstants.WalletCommandTypes.Generate:
                        eventEntries = await PlanGenerateEvents(
                            user, accountId, coinSymbol, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.WalletCommandTypes.Deposit:
                        eventEntries = await PlanDepositEvents(
                            user, accountId, coinSymbol, amount, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.WalletCommandTypes.Withdrawal:
                        eventEntries = await PlanWithdrawalEvents(
                            user, accountId, coinSymbol, amount, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.WalletCommandTypes.RevokeDeposit:
                        eventEntries = await PlanRevokeDepositEvents(
                            user, accountId, coinSymbol, walletEventIdReference, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.WalletCommandTypes.RevokeWithdrawal:
                        eventEntries = await PlanRevokeWithdrawalEvents(
                            user, accountId, coinSymbol, walletEventIdReference, requestId, reportInvalidMessage);
                        break;

                    default:
                        throw reportInvalidMessage($"Unrecognized wallet command type: {walletCommandType}");
                }

                _logger.LogInformation(
                    $"{(retry ? "Retrying" : "Trying")} to persist {eventEntries.Count.ToString()} event(s) planned by command requestId {requestId} on version number {eventEntries[0].VersionNumber.ToString()}");
                var success = await EventHistoryService.Persist(eventEntries);
                retry = success == null;
            }
            while (retry);

            _logger.LogInformation($"Successfully persisted events from requestId {requestId}");
        }

        private async Task<IList<EventEntry>> PlanGenerateEvents(
            string user, string accountId, string coinSymbol, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            var walletPublicKey = WalletOperationService.GetPublicKey(user, accountId, coinSymbol);
            if (walletPublicKey == null)
            {
                var hdSeed = await AbstractProvider.ProviderLookup[coinSymbol].GenerateHdWallet();
                walletPublicKey =
                    await AbstractProvider.ProviderLookup[coinSymbol].GetPublicKeyFromHdWallet(hdSeed);
                WalletOperationService.StoreHdWallet(hdSeed, walletPublicKey, user, accountId, coinSymbol);
            }

            var plannedEvents = new List<EventEntry>();
            var now = new DateTime();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                plannedEvents.Add(
                    new WalletGenerateEventEntry
                    {
                        VersionNumber = eventVersionNumber,
                        User = user,
                        AccountId = accountId,
                        CoinSymbol = coinSymbol,
                        EntryTime = now,
                        NewBalance = 0,
                        WalletPublicKey = walletPublicKey,
                    }
                );
            });
            return plannedEvents;
        }

        private async Task<IList<EventEntry>> PlanDepositEvents(
            string user, string accountId, string coinSymbol, decimal amount, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            throw new NotImplementedException();
        }

        private async Task<IList<EventEntry>> PlanWithdrawalEvents(
            string user, string accountId, string coinSymbol, decimal amount, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            throw new NotImplementedException();
        }

        private async Task<IList<EventEntry>> PlanRevokeDepositEvents(
            string user, string accountId, string coinSymbol, string walletEventIdReference, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            throw new NotImplementedException();
        }

        private async Task<IList<EventEntry>> PlanRevokeWithdrawalEvents(
            string user, string accountId, string coinSymbol, string walletEventIdReference, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            throw new NotImplementedException();
        }
    }
}
