using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors.Command
{
    public class WalletCommandProcessor
    {
        private readonly ILogger<WalletCommandProcessor> _logger;
        private EventHistoryService EventHistoryService { get; }

        /// <summary>
        /// Created via ProcessorFactory.
        /// </summary>
        public WalletCommandProcessor(EventHistoryService eventHistoryService, ILogger<WalletCommandProcessor> logger)
        {
            _logger = logger;
            EventHistoryService = eventHistoryService;
        }

        public async Task ExecuteWalletOperationCommand(
            string user, string accountId, string coinSymbol, string walletCommandType, decimal amount,
            string walletEventIdReference, string requestId, Func<string, Exception> reportInvalidMessage)
        {
            bool retry;
            do
            {
                IList<EventEntry> eventEntries;
                switch (walletCommandType)
                {
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

                var success = await EventHistoryService.Persist(eventEntries);
                retry = success == null;
            }
            while (retry);
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
