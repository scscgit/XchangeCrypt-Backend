using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.TradingService.Processors.Event;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors.Command
{
    public class TradeCommandProcessor
    {
        private readonly ILogger<TradeCommandProcessor> _logger;
        private VersionControl VersionControl { get; }
        private TradingOrderService TradingOrderService { get; }
        private UserService UserService { get; }
        private EventHistoryService EventHistoryService { get; }

        /// <summary>
        /// Created via ProcessorFactory.
        /// </summary>
        public TradeCommandProcessor(
            VersionControl versionControl,
            TradingOrderService tradingOrderService,
            UserService userService,
            EventHistoryService eventHistoryService,
            ILogger<TradeCommandProcessor> logger)
        {
            _logger = logger;
            VersionControl = versionControl;
            TradingOrderService = tradingOrderService;
            UserService = userService;
            EventHistoryService = eventHistoryService;
        }

        public async Task ExecuteTradeOrderCommand(
            string user, string accountId, string instrument, decimal? quantity, string side, string orderType,
            decimal? limitPrice, decimal? stopPrice, string durationType, decimal? duration, decimal? stopLoss,
            decimal? takeProfit, long? orderCreatedOnVersionNumber, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            var retry = false;
            do
            {
                IList<EventEntry> eventEntries;
                switch (orderType)
                {
                    case MessagingConstants.OrderTypes.LimitOrder:
                        eventEntries = await PlanLimitOrderEvents(
                            user, accountId, instrument, quantity.Value, ParseSide(side, reportInvalidMessage),
                            limitPrice.Value, durationType, duration, stopLoss, takeProfit, requestId,
                            reportInvalidMessage);
                        break;

                    case MessagingConstants.OrderTypes.StopOrder:
                        eventEntries = await PlanStopOrderEvents(
                            user, accountId, instrument, quantity.Value, ParseSide(side, reportInvalidMessage),
                            stopPrice.Value, durationType, duration, stopLoss, takeProfit, requestId,
                            reportInvalidMessage);
                        break;

                    case MessagingConstants.OrderTypes.MarketOrder:
                        eventEntries = await PlanMarketOrderEvents(
                            user, accountId, instrument, quantity.Value, ParseSide(side, reportInvalidMessage),
                            durationType, duration, stopLoss, takeProfit, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.OrderTypes.Cancel:
                        eventEntries = await PlanCancelOrder(
                            user, accountId, orderCreatedOnVersionNumber.Value, requestId,
                            reportInvalidMessage);
                        break;

                    default:
                        throw reportInvalidMessage($"Unrecognized order type: {orderType}");
                }

                _logger.LogInformation(
                    $"{(retry ? "Retrying" : "Trying")} to persist {eventEntries.Count.ToString()} event(s) planned by {orderType} command requestId {requestId} on version number {eventEntries[0].VersionNumber.ToString()}");
                retry = null == await EventHistoryService.Persist(eventEntries);
            }
            while (retry);

            _logger.LogInformation($"Successfully persisted events from requestId {requestId}");
        }

        private async Task<IList<EventEntry>> PlanCancelOrder(
            string user, string accountId, long orderCreatedOnVersionNumber, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            var plannedEvents = new List<EventEntry>();
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                object openOrder;
                try
                {
                    openOrder = TradingOrderService.FindOpenOrderCreatedByVersionNumber(
                        orderCreatedOnVersionNumber
                    );
                }
                catch (InvalidOperationException)
                {
                    openOrder = null;
                }

                if (openOrder is OrderBookEntry limitOrder)
                {
                    if (!user.Equals(limitOrder.User)
                        || !accountId.Equals(limitOrder.AccountId))
                    {
                        throw reportInvalidMessage("Couldn't cancel limit order, user or accountId differs");
                    }

                    plannedEvents.Add(new CancelOrderEventEntry
                    {
                        VersionNumber = eventVersionNumber,
                        User = user,
                        AccountId = accountId,
                        Instrument = limitOrder.Instrument,
                        CancelOrderCreatedOnVersionNumber = orderCreatedOnVersionNumber,
                    });
                    return;
                }

                if (openOrder is HiddenOrderEntry stopOrder)
                {
                    if (!user.Equals(stopOrder.User)
                        || !accountId.Equals(stopOrder.AccountId))
                    {
                        throw reportInvalidMessage("Couldn't cancel stop order, user or accountId differs");
                    }

                    plannedEvents.Add(new CancelOrderEventEntry
                    {
                        VersionNumber = eventVersionNumber,
                        User = user,
                        AccountId = accountId,
                        Instrument = stopOrder.Instrument,
                        CancelOrderCreatedOnVersionNumber = orderCreatedOnVersionNumber,
                    });
                    return;
                }

                throw reportInvalidMessage("Couldn't find a matching limit or stop order to close");
            });
            return plannedEvents;
        }

        private async Task<IList<EventEntry>> PlanLimitOrderEvents(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide,
            decimal limitPriceValue, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit,
            string requestId, Func<string, Exception> reportInvalidMessage)
        {
            if (quantity <= 0)
            {
                throw reportInvalidMessage("You cannot create an order with a quantity of 0 or less");
            }

            if (limitPriceValue <= 0
                || stopLoss.HasValue && stopLoss.Value <= 0
                || takeProfit.HasValue && takeProfit.Value <= 0)
            {
                throw reportInvalidMessage("You cannot create a limit order with a price of 0 or less");
            }

            var limitOrderEvent = new CreateOrderEventEntry
            {
                User = user,
                AccountId = accountId,
                Instrument = instrument,
                Qty = quantity,
                Side = orderSide,
                Type = OrderType.Limit,
                LimitPrice = limitPriceValue,
                StopPrice = null,
                DurationType = durationType,
                Duration = duration,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
            };
            var plannedEvents = new List<EventEntry>
            {
                limitOrderEvent
            };
            var finished = false;
            // Note: using async prefix on the lambda caused it to never finish!
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                // We are now locked to a specific version number
                limitOrderEvent.VersionNumber = eventVersionNumber;

                AssertUnreservedBalance(
                    user, accountId, instrument, quantity, orderSide, limitPriceValue, reportInvalidMessage);

                PlanMatchOrdersLocked(
                    user, accountId, instrument, orderSide, limitPriceValue, quantity, eventVersionNumber,
                    requestId, reportInvalidMessage
                ).Item1.ForEach(plannedEvents.Add);

                finished = true;
            });

            if (finished == false)
            {
                throw new Exception($"{nameof(VersionControl.ExecuteUsingFixedVersion)} didn't finish");
            }

            return plannedEvents;
        }

        private void AssertUnreservedBalance(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide, decimal? price,
            Func<string, Exception> reportInvalidMessage)
        {
            var sideQuantity = orderSide == OrderSide.Buy ? quantity * price.Value : quantity;
            // Make sure that it even makes sense to open this order
            var paymentCurrency = instrument.Split("_")[orderSide == OrderSide.Buy ? 1 : 0];
            var (balance, reservedBalance) =
                UserService.GetBalanceAndReservedBalance(user, accountId, paymentCurrency);
            if (sideQuantity > balance - reservedBalance)
            {
                throw reportInvalidMessage(
                    $"Cannot open the order, your available balance of {balance - reservedBalance} {paymentCurrency} is less than {sideQuantity} {paymentCurrency} required. Please close some orders");
            }
        }

        private (List<MatchOrderEventEntry>, decimal) PlanMatchOrdersLocked(
            string user, string accountId, string instrument, OrderSide orderSide, decimal? limitPriceValue,
            decimal quantityRemaining, long lockedEventVersionNumber, string requestId,
            Func<string, Exception> reportInvalidMessage)
        {
            var plannedEvents = new List<MatchOrderEventEntry>();
            var baseCurrency = instrument.Split("_")[0];
            var quoteCurrency = instrument.Split("_")[1];

            // Start the process of matching relevant offers
            var matchingOffers = orderSide == OrderSide.Buy
                ? TradingOrderService.MatchSellers(limitPriceValue, instrument).Result
                : TradingOrderService.MatchBuyers(limitPriceValue, instrument).Result;
            matchingOffers.MoveNext();
            var matchingOfferBatch = matchingOffers.Current.ToList();
            while (quantityRemaining > 0 && matchingOfferBatch.Count > 0)
            {
                _logger.LogInformation(
                    $"Request {requestId} matched a batch of {matchingOfferBatch.Count} {(orderSide == OrderSide.Buy ? "buyers" : "sellers")}");
                foreach (var other in matchingOfferBatch)
                {
                    var otherRemaining = other.Qty - other.FilledQty;
                    decimal matchedQuantity;
                    if (otherRemaining >= quantityRemaining)
                    {
                        // Entire command order remainder is consumed by the seller offer
                        matchedQuantity = quantityRemaining;
                        _logger.LogInformation(
                            $"New {instrument} {(orderSide == OrderSide.Buy ? "buy" : "sell")} limit order planning entirely matched order id {other.Id}");
                    }
                    else
                    {
                        // Fraction of order will remain, but the seller offer will be consumed
                        matchedQuantity = otherRemaining;
                        _logger.LogInformation(
                            $"New {instrument} {(orderSide == OrderSide.Buy ? "buy" : "sell")} limit order planning partially matched order id {other.Id}");
                    }

                    quantityRemaining -= matchedQuantity;

                    var matchEvent = new MatchOrderEventEntry
                    {
                        VersionNumber = lockedEventVersionNumber,
                        ActionUser = user,
                        ActionAccountId = accountId,
                        TargetOrderOnVersionNumber = other.CreatedOnVersionId,
                        TargetUser = other.User,
                        TargetAccountId = other.AccountId,
                        Instrument = instrument,
                        Qty = matchedQuantity,
                        ActionSide = orderSide,
                        Price = other.LimitPrice,
                        ActionOrderQtyRemaining = quantityRemaining,
                        TargetOrderQtyRemaining = other.Qty - other.FilledQty - matchedQuantity,
                    };

                    // Calculating new balances for double-check purposes
                    var (actionBaseMod, actionQuoteMod, targetBaseMod, targetQuoteMod) =
                        TradeEventProcessor.MatchOrderBalanceModifications(matchEvent);
                    try
                    {
                        matchEvent.ActionBaseNewBalance =
                            UserService.GetBalanceAndReservedBalance(
                                matchEvent.ActionUser, matchEvent.ActionAccountId, baseCurrency
                            ).Item1 + actionBaseMod;
                        matchEvent.ActionQuoteNewBalance =
                            UserService.GetBalanceAndReservedBalance(
                                matchEvent.ActionUser, matchEvent.ActionAccountId, quoteCurrency
                            ).Item1 + actionQuoteMod;
                        matchEvent.TargetBaseNewBalance =
                            UserService.GetBalanceAndReservedBalance(
                                matchEvent.TargetUser, matchEvent.TargetAccountId, baseCurrency
                            ).Item1 + targetBaseMod;
                        matchEvent.TargetQuoteNewBalance =
                            UserService.GetBalanceAndReservedBalance(
                                matchEvent.TargetUser, matchEvent.TargetAccountId, quoteCurrency
                            ).Item1 + targetQuoteMod;
                    }
                    catch (Exception e)
                    {
                        // This can happen if a user didn't generate his balances yet, so it's not a fatal error
                        throw reportInvalidMessage(
                            $"There was a problem with your coin balances. {e.GetType().Name}: {e.Message}");
                    }

                    plannedEvents.Add(matchEvent);
                    if (quantityRemaining == 0)
                    {
                        break;
                    }
                }

                if (quantityRemaining == 0)
                {
                    break;
                }


                // Keep the iteration going in order to find further matching orders as long as remaining qty > 0
                if (!matchingOffers.MoveNext())
                {
                    break;
                }

                matchingOfferBatch = matchingOffers.Current.ToList();
            }

            return (plannedEvents, quantityRemaining);
        }

        private async Task<IList<EventEntry>> PlanStopOrderEvents(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide,
            decimal stopPriceValue, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit,
            string requestId, Func<string, Exception> reportInvalidMessage)
        {
            if (quantity <= 0)
            {
                throw reportInvalidMessage("You cannot create an order with a quantity of 0 or less");
            }

            if (stopPriceValue <= 0
                || stopLoss.HasValue && stopLoss.Value <= 0
                || takeProfit.HasValue && takeProfit.Value <= 0)
            {
                throw reportInvalidMessage("You cannot create a stop order with a price of 0 or less");
            }

            var stopOrderEvent = new CreateOrderEventEntry
            {
                User = user,
                AccountId = accountId,
                Instrument = instrument,
                Qty = quantity,
                Side = orderSide,
                Type = OrderType.Stop,
                LimitPrice = null,
                StopPrice = stopPriceValue,
                DurationType = durationType,
                Duration = duration,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
            };
            var plannedEvents = new List<EventEntry>
            {
                stopOrderEvent
            };
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                stopOrderEvent.VersionNumber = eventVersionNumber;

                AssertUnreservedBalance(
                    user, accountId, instrument, quantity, orderSide, stopPriceValue, reportInvalidMessage);
            });
            return plannedEvents;
        }

        private async Task<IList<EventEntry>> PlanMarketOrderEvents(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide,
            string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit,
            string requestId, Func<string, Exception> reportInvalidMessage)
        {
            if (quantity <= 0)
            {
                throw reportInvalidMessage("You cannot create an order with a quantity of 0 or less");
            }

            if (stopLoss.HasValue && stopLoss.Value <= 0
                || takeProfit.HasValue && takeProfit.Value <= 0)
            {
                throw reportInvalidMessage("You cannot create a market order with a price of 0 or less");
            }

            var marketOrderEvent = new CreateOrderEventEntry
            {
                User = user,
                AccountId = accountId,
                Instrument = instrument,
                Qty = quantity,
                //FilledMarketOrderQty
                Side = orderSide,
                Type = OrderType.Market,
                //LimitPrice,
                StopPrice = null,
                DurationType = durationType,
                Duration = duration,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
            };
            var plannedEvents = new List<EventEntry>
            {
                marketOrderEvent
            };
            VersionControl.ExecuteUsingFixedVersion(currentVersionNumber =>
            {
                var eventVersionNumber = currentVersionNumber + 1;
                marketOrderEvent.VersionNumber = eventVersionNumber;

                // In the case of buying we cannot be sure that the entire quantity will get matched
                if (orderSide == OrderSide.Sell)
                {
                    AssertUnreservedBalance(
                        user, accountId, instrument, quantity, orderSide, null, reportInvalidMessage);
                }

                var matchedOrders = PlanMatchOrdersLocked(
                    user, accountId, instrument, orderSide, null, quantity, eventVersionNumber,
                    requestId, reportInvalidMessage
                );
                if (matchedOrders.Item1.Count == 0)
                {
                    throw reportInvalidMessage("Cannot execute a market order, as there are no other orders");
                }

                matchedOrders.Item1.ForEach(plannedEvents.Add);
                marketOrderEvent.FilledMarketOrderQty = quantity - matchedOrders.Item2;
                marketOrderEvent.LimitPrice = matchedOrders.Item1.Last().Price;
            });
            return plannedEvents;
        }

        private static OrderSide ParseSide(string side, Func<string, Exception> reportInvalidMessage)
        {
            switch (side)
            {
                case MessagingConstants.OrderSides.BuySide:
                    return OrderSide.Buy;

                case MessagingConstants.OrderSides.SellSide:
                    return OrderSide.Sell;

                default:
                    throw reportInvalidMessage($"Unrecognized order side: {side}");
            }
        }
    }
}
