using System;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
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
            // This event was created by Wallet Service, so that it couldn't really access user's reserved balance.
            // You may ask, how can we make sure no withdrawal occurs concurrently during a trade order creation?
            // The trade command processor has strict sequential rules => such trade event persistence won't be allowed.
            // Another issue occurs when a user has balance reserved in an open position.
            // In that case we close his positions, so the trade command processor no longer uses them in calculation.

            var overdrawn = eventEntry.OverdrawnAndCanceledOrders;
            if (!overdrawn)
            {
                var (balance, reservedBalance) = _userService
                    .GetBalanceAndReservedBalance(eventEntry.User, eventEntry.AccountId, eventEntry.CoinSymbol);
                if (balance - reservedBalance < eventEntry.WithdrawalQty)
                {
                    // First time the event has detected an overdraw, it will be persisted for reference purposes
                    // (and for a marginal calculation speedup next time the event gets processed after restart)
                    _eventHistoryService.ReportOverdrawnWithdrawal(eventEntry);
                    overdrawn = true;
                }
            }

            if (overdrawn)
            {
                _logger.LogInformation(
                    $"User {eventEntry.User} accountId {eventEntry.AccountId} has overdrawn his {eventEntry.CoinSymbol} balance by a withdrawal so large, that all his positions need to be closed");
                decimal unreservedBalance = 0;
                foreach (var (baseCurrency, quoteCurrency)
                    in _tradingOrderService.GetInstruments(eventEntry.CoinSymbol))
                {
                    var instrument = $"{baseCurrency}_{quoteCurrency}";
                    // We only cancel the really relevant active orders - though for simplicity we cancel them all
                    OrderSide cancelOrderSide;
                    if (eventEntry.CoinSymbol.Equals(baseCurrency))
                    {
                        cancelOrderSide = OrderSide.Sell;
                    }
                    else if (eventEntry.CoinSymbol.Equals(quoteCurrency))
                    {
                        cancelOrderSide = OrderSide.Buy;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    foreach (var orderBookEntry
                        in _tradingOrderService.GetLimitOrders(
                            eventEntry.User, eventEntry.AccountId, instrument, cancelOrderSide))
                    {
                        unreservedBalance +=
                            _tradingOrderService.CancelOrderById(orderBookEntry.Id, eventEntry.EntryTime);
                        _logger.LogInformation(
                            $"Canceling {instrument} limit order due to balance overdraw, id {orderBookEntry.Id} (unreserved a rolling total of {unreservedBalance} {eventEntry.CoinSymbol})");
                    }

                    // Stop orders will get canceled too
                    foreach (var stopOrderEntry
                        in _tradingOrderService.GetStopOrders(
                            eventEntry.User, eventEntry.AccountId, instrument, cancelOrderSide))
                    {
                        unreservedBalance +=
                            _tradingOrderService.CancelOrderById(stopOrderEntry.Id, eventEntry.EntryTime);
                        _logger.LogInformation(
                            $"Canceling {instrument} stop order due to balance overdraw, id {stopOrderEntry.Id} (unreserved a rolling total of {unreservedBalance} {eventEntry.CoinSymbol})");
                    }
                }

                _userService.ModifyReservedBalance(
                    eventEntry.User,
                    eventEntry.AccountId,
                    eventEntry.CoinSymbol,
                    -unreservedBalance);
            }

            _userService.ModifyBalance(
                eventEntry.User,
                eventEntry.AccountId,
                eventEntry.CoinSymbol,
                -eventEntry.WithdrawalQty);
        }
    }
}
