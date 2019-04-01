using System;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors.Event
{
    public class TradeEventProcessor
    {
        private readonly TradingOrderService _tradingOrderService;
        private readonly EventHistoryService _eventHistoryService;
        private readonly UserService _userService;
        private readonly ILogger<TradeEventProcessor> _logger;

        public TradeEventProcessor(
            TradingOrderService tradingOrderService,
            EventHistoryService eventHistoryService,
            UserService userService,
            ILogger<TradeEventProcessor> logger)
        {
            _tradingOrderService = tradingOrderService;
            _eventHistoryService = eventHistoryService;
            _userService = userService;
            _logger = logger;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        // ReSharper disable once UnusedParameter.Global
        public void ProcessEvent(EventEntry eventEntry)
        {
            throw new NotSupportedException();
        }

        public void ProcessEvent(CancelOrderEventEntry eventEntry)
        {
            _tradingOrderService.CancelOrder(eventEntry);
        }

        public void ProcessEvent(CreateOrderEventEntry eventEntry)
        {
            _tradingOrderService.CreateOrder(eventEntry);
        }

        public void ProcessEvent(MatchOrderEventEntry eventEntry)
        {
            _tradingOrderService.MatchOrder(eventEntry);
        }

        public void ProcessEvent(TransactionCommitEventEntry eventEntry)
        {
            // Ignored
        }

        public void ProcessEvent(WalletGenerateEventEntry eventEntry)
        {
            _userService.AddWallet(
                eventEntry.User, eventEntry.AccountId, eventEntry.CoinSymbol, eventEntry.LastWalletPublicKey);
        }

        public void ProcessEvent(WalletDepositEventEntry eventEntry)
        {
            _userService.ModifyBalance(
                eventEntry.User,
                eventEntry.AccountId,
                eventEntry.CoinSymbol,
                eventEntry.DepositQty);
        }

        public void ProcessEvent(WalletRevokeEventEntry eventEntry)
        {
            var relativeBalance = 0m;
            var revoke = _eventHistoryService.FindById(eventEntry.RevokeWalletEventEntryId);
            switch (revoke)
            {
                case WalletDepositEventEntry deposit:
                    relativeBalance = -deposit.DepositQty;
                    break;
                case WalletWithdrawalEventEntry withdrawal:
                    relativeBalance = withdrawal.WithdrawalQty;
                    break;
                default:
                    throw new NotImplementedException();
            }

            _userService.ModifyBalance(
                eventEntry.User,
                eventEntry.AccountId,
                eventEntry.CoinSymbol,
                relativeBalance);
        }

        public void ProcessEvent(WalletWithdrawalEventEntry eventEntry)
        {
            throw new NotImplementedException();
        }
    }
}
