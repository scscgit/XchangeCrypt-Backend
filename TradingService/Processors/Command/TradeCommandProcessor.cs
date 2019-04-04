using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.DatabaseAccess.Control;
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
            string user, string accountId, string instrument, decimal quantity, string side, string orderType,
            decimal? limitPrice, decimal? stopPrice, string durationType, decimal? duration, decimal? stopLoss,
            decimal? takeProfit, string requestId, Func<string, Exception> reportInvalidMessage)
        {
            var orderSideOptional = ParseSide(side);
            if (!orderSideOptional.HasValue)
            {
                throw reportInvalidMessage($"Unrecognized order side: {side}");
            }

            var orderSide = orderSideOptional.Value;

            var retry = false;
            do
            {
                IList<EventEntry> eventEntries;
                switch (orderType)
                {
                    case MessagingConstants.OrderTypes.LimitOrder:
                        eventEntries = await PlanLimitOrderEvents(
                            user, accountId, instrument, quantity, orderSide, limitPrice.Value, durationType, duration,
                            stopLoss, takeProfit, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.OrderTypes.StopOrder:
                        eventEntries = await PlanStopOrderEvents(
                            user, accountId, instrument, quantity, orderSide, stopPrice.Value, durationType, duration,
                            stopLoss, takeProfit, requestId, reportInvalidMessage);
                        break;

                    case MessagingConstants.OrderTypes.MarketOrder:
                        eventEntries = await PlanMarketOrderEvents(
                            user, accountId, instrument, quantity, orderSide, durationType, duration, stopLoss,
                            takeProfit, requestId, reportInvalidMessage);
                        break;

                    default:
                        throw reportInvalidMessage($"Unrecognized order type: {orderType}");
                }

                _logger.LogInformation(
                    $"{(retry ? "Retrying" : "Trying")} to persist {eventEntries.Count.ToString()} {orderType} event(s) planned by command requestId {requestId} on version number {eventEntries[0].VersionNumber.ToString()}");
                retry = null == await EventHistoryService.Persist(eventEntries);
            }
            while (retry);

            _logger.LogInformation($"Successfully persisted events from requestId {requestId}");
        }

        private async Task<IList<EventEntry>> PlanLimitOrderEvents(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide,
            decimal limitPriceValue, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit,
            string requestId, Func<string, Exception> reportInvalidMessage)
        {
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
                var quantityRemaining = quantity;

                // Start the process of matching relevant offers
                var matchingOffers = orderSide == OrderSide.Buy
                    ? TradingOrderService.MatchSellers(limitPriceValue, instrument).Result
                    : TradingOrderService.MatchBuyers(limitPriceValue, instrument).Result;
                matchingOffers.MoveNext();
                var matchingOfferBatch = matchingOffers.Current.ToList();
                while (quantityRemaining > 0 && matchingOfferBatch.Count > 0)
                {
                    _logger.LogInformation(
                        $"Limit order request {requestId} matched a batch of {matchingOfferBatch.Count} {(orderSide == OrderSide.Buy ? "buyers" : "sellers")}");
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
                            VersionNumber = eventVersionNumber,
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
                        var currencies = instrument.Split("_");
                        var baseCurrency = currencies[0];
                        var quoteCurrency = currencies[1];
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

                finished = true;
            });

            if (finished == false)
            {
                throw new Exception($"{nameof(VersionControl.ExecuteUsingFixedVersion)} didn't finish");
            }

            return plannedEvents;
        }

        private async Task<IList<EventEntry>> PlanStopOrderEvents(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide,
            decimal stopPriceValue, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit,
            string requestId, Func<string, Exception> reportInvalidMessage)
        {
            throw new NotImplementedException();
        }

        private async Task<IList<EventEntry>> PlanMarketOrderEvents(
            string user, string accountId, string instrument, decimal quantity, OrderSide orderSide,
            string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit,
            string requestId, Func<string, Exception> reportInvalidMessage)
        {
            throw new NotImplementedException();
        }

        private static OrderSide? ParseSide(string side)
        {
            switch (side)
            {
                case MessagingConstants.OrderSides.BuySide:
                    return OrderSide.Buy;

                case MessagingConstants.OrderSides.SellSide:
                    return OrderSide.Sell;

                default:
                    return null;
            }
        }
    }
}
