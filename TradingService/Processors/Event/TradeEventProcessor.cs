using System;
using System.Linq;
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

        public static (decimal, decimal, decimal, decimal) MatchOrderBalanceModifications(
            MatchOrderEventEntry eventEntry)
        {
            int actionUserBaseModifier;
            int actionUserQuoteModifier;
            switch (eventEntry.ActionSide)
            {
                case OrderSide.Buy:
                    actionUserBaseModifier = 1;
                    actionUserQuoteModifier = -1;
                    break;
                case OrderSide.Sell:
                    actionUserBaseModifier = -1;
                    actionUserQuoteModifier = 1;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            // Action user Base
            // Action user Quote
            // Target user Base
            // Target user Quote
            return (
                actionUserBaseModifier * eventEntry.Qty,
                actionUserQuoteModifier * eventEntry.Qty * eventEntry.Price,
                -actionUserBaseModifier * eventEntry.Qty,
                -actionUserQuoteModifier * eventEntry.Qty * eventEntry.Price);
        }

        public void ProcessEvent(MatchOrderEventEntry eventEntry)
        {
            _tradingOrderService.MatchOrder(eventEntry);
            var currencies = eventEntry.Instrument.Split("_");
            var baseCurrency = currencies[0];
            var quoteCurrency = currencies[1];
            var (actionBase, actionQuote, targetBase, targetQuote) = MatchOrderBalanceModifications(eventEntry);

            _userService.ModifyBalance(
                eventEntry.ActionUser,
                eventEntry.ActionAccountId,
                baseCurrency,
                actionBase);

            _userService.ModifyBalance(
                eventEntry.ActionUser,
                eventEntry.ActionAccountId,
                quoteCurrency,
                actionQuote);

            _userService.ModifyBalance(
                eventEntry.TargetUser,
                eventEntry.TargetAccountId,
                baseCurrency,
                targetBase);

            _userService.ModifyBalance(
                eventEntry.TargetUser,
                eventEntry.TargetAccountId,
                quoteCurrency,
                targetQuote);
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
            var revoked = _eventHistoryService.FindById(eventEntry.RevokeWalletEventEntryId);
            switch (revoked)
            {
                case WalletDepositEventEntry deposit:
                    relativeBalance = -deposit.DepositQty;
                    break;
                case WalletWithdrawalEventEntry withdrawal:
                    if (withdrawal.Validated == false)
                    {
                        // Not revoking event that was not executed
                        return;
                    }

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

        public void ProcessEvent(WalletConsolidationTransferEventEntry eventEntry)
        {
            if (eventEntry.Valid != null)
            {
                // Consolidation was either invalid anyway, or it's already validated
                return;
            }

            // Validate the consolidation's withdrawal - Wallet Server actively waits for this!
            var currentVersionNumberEvents = _eventHistoryService.FindByVersionNumber(eventEntry.VersionNumber)
                .ToList();
            var consolidationList = currentVersionNumberEvents.FindAll(e => e is WalletConsolidationTransferEventEntry);
            var withdrawalList = currentVersionNumberEvents.FindAll(e => e is WalletWithdrawalEventEntry);
            if (withdrawalList.Count == 0)
            {
                throw new Exception("Standalone consolidation not implemented yet");
            }

            var withdrawal = (WalletWithdrawalEventEntry) withdrawalList.Single();
            bool valid;
            if (withdrawal.Validated.HasValue)
            {
                valid = withdrawal.Validated.Value;
            }
            else
            {
                var (balance, reservedBalance) = _userService
                    .GetBalanceAndReservedBalance(eventEntry.User, eventEntry.AccountId, eventEntry.CoinSymbol);
                if (!IsValidWithdrawal(withdrawal, balance))
                {
                    valid = false;
                    // Withdrawal is invalid
                    _eventHistoryService.ReportWithdrawalValidation(withdrawal, valid);
                }
                else
                {
                    valid = true;
                    _eventHistoryService.ReportWithdrawalValidation(withdrawal, valid);
                }
            }

            foreach (var consolidationEntry in consolidationList)
            {
                var consolidation = (WalletConsolidationTransferEventEntry) consolidationEntry;
                _eventHistoryService.ReportConsolidationValidated(consolidation, valid);
            }
        }

        public void ProcessEvent(WalletWithdrawalEventEntry eventEntry)
        {
            // This event was created by Wallet Service, so that it couldn't really access user's reserved balance.
            // You may ask, how can we make sure no withdrawal occurs concurrently during a trade order creation?
            // The trade command processor has strict sequential rules => such trade event persistence won't be allowed.
            // Another issue occurs when a user has balance reserved in an open position.
            // In that case we close his positions, so the trade command processor no longer uses them in calculation.
            // Lastly, there is an issue with Wallet Service not having access to the user's current balance.
            // This is solved using a Saga approach, so that we must validate the Withdrawal Event entry here,
            // while the Wallet Service actively waits using a parallel thread. If the thread ever gets killed,
            // the withdrawal process will get stuck, waiting for administrator to consolidate such transaction.
            // In the future, an alternative approach to unstuck such ignored withdrawals can be added.

            if (eventEntry.Validated == false)
            {
                // Withdrawal was invalid anyway
                return;
            }

            var overdrawn = eventEntry.OverdrawnAndCanceledOrders;
            if (!overdrawn || eventEntry.Validated == null)
            {
                var (balance, reservedBalance) = _userService
                    .GetBalanceAndReservedBalance(eventEntry.User, eventEntry.AccountId, eventEntry.CoinSymbol);
                if (eventEntry.Validated == null)
                {
                    if (!IsValidWithdrawal(eventEntry, balance))
                    {
                        _eventHistoryService.ReportWithdrawalValidation(eventEntry, false);
                        // Withdrawal is invalid
                        return;
                    }

                    _eventHistoryService.ReportWithdrawalValidation(eventEntry, true);
                }

                if (!overdrawn && balance - reservedBalance < eventEntry.WithdrawalQty)
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
                        unreservedBalance += _tradingOrderService.CancelOrderByCreatedOnVersionNumber(
                            orderBookEntry.CreatedOnVersionId,
                            eventEntry.EntryTime
                        );
                        _logger.LogInformation(
                            $"Canceling {instrument} limit order due to balance overdraw, id {orderBookEntry.Id} (unreserved a rolling total of {unreservedBalance} {eventEntry.CoinSymbol})");
                    }

                    // Stop orders will get canceled too
                    foreach (var stopOrderEntry
                        in _tradingOrderService.GetStopOrders(
                            eventEntry.User, eventEntry.AccountId, instrument, cancelOrderSide))
                    {
                        unreservedBalance += _tradingOrderService.CancelOrderByCreatedOnVersionNumber(
                            stopOrderEntry.CreatedOnVersionId,
                            eventEntry.EntryTime
                        );
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
                -eventEntry.WithdrawalQty - eventEntry.WithdrawalCombinedFee);
        }

        private bool IsValidWithdrawal(WalletWithdrawalEventEntry eventEntry, decimal userCoinBalance)
        {
            return userCoinBalance >= eventEntry.WithdrawalQty + eventEntry.WithdrawalCombinedFee;
        }
    }
}
