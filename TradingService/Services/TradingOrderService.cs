using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;

namespace XchangeCrypt.Backend.TradingService.Services
{
    // TODO: one instance per instrument?
    public class TradingOrderService
    {
        private readonly EventHistoryService _eventHistoryService;
        private readonly ILogger<TradingOrderService> _logger;
        private IMongoCollection<OrderBookEntry> OrderBook { get; }
        private IMongoCollection<HiddenOrderEntry> HiddenOrders { get; }
        private IMongoCollection<OrderHistoryEntry> OrderHistory { get; }
        private IMongoCollection<TransactionHistoryEntry> TransactionHistory { get; }

        /// <summary>
        /// </summary>
        public TradingOrderService(
            TradingRepository tradingRepository,
            EventHistoryService eventHistoryService,
            ILogger<TradingOrderService> logger)
        {
            _eventHistoryService = eventHistoryService;
            _logger = logger;
            OrderBook = tradingRepository.OrderBook();
            HiddenOrders = tradingRepository.HiddenOrders();
            OrderHistory = tradingRepository.OrderHistory();
            TransactionHistory = tradingRepository.TransactionHistory();
        }

        internal List<(string, string)> GetInstruments(string coinSymbol)
        {
            return GlobalConfiguration.Instruments.Where(
                instrument =>
                    coinSymbol.Equals(instrument.Item1) || coinSymbol.Equals(instrument.Item2)
            ).ToList();
        }

        internal List<OrderBookEntry> GetLimitOrders(
            string user, string accountId, string instrument, OrderSide orderSide)
        {
            return OrderBook.Find(order =>
                order.User.Equals(user)
                && order.AccountId.Equals(accountId)
                && order.Instrument.Equals(instrument)
                && order.Side == orderSide
            ).ToList();
        }

        internal List<HiddenOrderEntry> GetStopOrders(
            string user, string accountId, string instrument, OrderSide orderSide)
        {
            return HiddenOrders.Find(order =>
                order.User.Equals(user)
                && order.AccountId.Equals(accountId)
                && order.Instrument.Equals(instrument)
                && order.Side == orderSide
            ).ToList();
        }

        internal void CreateOrder(CreateOrderEventEntry createOrder)
        {
            _logger.LogDebug("Called create order @ version number " + createOrder.VersionNumber);
            switch (createOrder.Type)
            {
                case OrderType.Limit:
                    var limitOrder = new OrderBookEntry
                    {
                        EntryTime = createOrder.EntryTime,
                        CreatedOnVersionId = createOrder.VersionNumber,
                        User = createOrder.User,
                        AccountId = createOrder.AccountId,
                        Instrument = createOrder.Instrument,
                        Qty = createOrder.Qty,
                        Side = createOrder.Side,
                        FilledQty = 0m,
                        LimitPrice = createOrder.LimitPrice.Value,
                        // TODO from stop loss and take profit
                        //ChildrenIds
                        DurationType = createOrder.DurationType,
                        Duration = createOrder.Duration,
                    };
                    OrderBook.InsertOne(limitOrder);
                    break;
                case OrderType.Stop:
                    var stopOrder = new HiddenOrderEntry
                    {
                        EntryTime = createOrder.EntryTime,
                        CreatedOnVersionId = createOrder.VersionNumber,
                        User = createOrder.User,
                        AccountId = createOrder.AccountId,
                        Instrument = createOrder.Instrument,
                        Qty = createOrder.Qty,
                        Side = createOrder.Side,
                        StopPrice = createOrder.StopPrice.Value,
                        // TODO from stop loss and take profit
                        //ChildrenIds
                        DurationType = createOrder.DurationType,
                        Duration = createOrder.Duration,
                    };
                    HiddenOrders.InsertOne(stopOrder);
                    break;
                case OrderType.Market:
                    OrderHistory.InsertOne(
                        new OrderHistoryEntry
                        {
                            CreateTime = createOrder.EntryTime,
                            CloseTime = createOrder.EntryTime,
                            User = createOrder.User,
                            AccountId = createOrder.AccountId,
                            Instrument = createOrder.Instrument,
                            Qty = createOrder.Qty,
                            Side = createOrder.Side,
                            // Closed market order
                            Type = OrderType.Market,
                            // The market order will be filled by the following match orders
                            FilledQty = createOrder.FilledMarketOrderQty,
                            // The most expensive accepted limit price will be shown to the user
                            LimitPrice = createOrder.LimitPrice.Value,
                            StopPrice = null,
                            // TODO from stop loss and take profit
                            //ChildrenIds
                            DurationType = createOrder.DurationType,
                            Duration = createOrder.Duration,
                            Status = OrderStatus.Filled,
                        }
                    );
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(createOrder.Type));
            }

            _logger.LogDebug("Persisted create order @ version number " + createOrder.VersionNumber);
        }

        internal void MatchOrder(MatchOrderEventEntry matchOrder)
        {
            _logger.LogDebug("Called match order @ version number " + matchOrder.VersionNumber);

            // Old incorrect way:
            // In order to find actionOrderId, we must go a little roundabout way
//            var matchOrderRelatedCreateOrder = _eventHistoryRepository.Events<CreateOrderEventEntry>().Find(
//                Builders<CreateOrderEventEntry>.Filter.Eq(e => e.VersionNumber, matchOrder.VersionNumber)
//            ).First();
//            var actionOrderId = matchOrderRelatedCreateOrder.Id;

            var now = matchOrder.EntryTime;
            // Action order is not used in case it's a market order
            var actionOrder = OrderBook.Find(
                Builders<OrderBookEntry>.Filter.Eq(e => e.CreatedOnVersionId, matchOrder.VersionNumber)
            ).SingleOrDefault();
            var targetOrder = OrderBook.Find(
                Builders<OrderBookEntry>.Filter.Eq(e => e.CreatedOnVersionId, matchOrder.TargetOrderOnVersionNumber)
            ).Single();
            AssertMatchOrderQty(matchOrder, actionOrder, targetOrder);

            if (actionOrder != null)
            {
                if (matchOrder.ActionOrderQtyRemaining == 0)
                {
                    OrderBook.DeleteOne(
                        Builders<OrderBookEntry>.Filter.Eq(e => e.CreatedOnVersionId, matchOrder.VersionNumber)
                    );
                    // The entire order quantity was filled
                    InsertOrderHistoryEntry(actionOrder.Qty, actionOrder, OrderStatus.Filled, now);
                }
                else
                {
                    OrderBook.UpdateOne(
                        Builders<OrderBookEntry>.Filter.Eq(e => e.CreatedOnVersionId, matchOrder.VersionNumber),
                        Builders<OrderBookEntry>.Update.Set(
                            e => e.FilledQty, actionOrder.Qty - matchOrder.ActionOrderQtyRemaining)
                    );
                }
            }
            else if (
                // This condition can be completely deleted without a worry, it's just a double-check
                ((CreateOrderEventEntry) _eventHistoryService
                    .FindByVersionNumber(matchOrder.VersionNumber)
                    .First()
                ).Type != OrderType.Market
            )
            {
                // We don't have to update the OrderHistoryEntry, because it already contains the matched quantity
                throw new Exception(
                    $"Match order id {matchOrder.Id} did not have action being a limit order, and it was not a market order");
            }

            if (matchOrder.TargetOrderQtyRemaining == 0)
            {
                OrderBook.DeleteOne(
                    Builders<OrderBookEntry>.Filter.Eq(e => e.CreatedOnVersionId, matchOrder.TargetOrderOnVersionNumber)
                );
                // The entire order quantity was filled
                InsertOrderHistoryEntry(targetOrder.Qty, targetOrder, OrderStatus.Filled, now);
            }
            else
            {
                OrderBook.UpdateOne(
                    Builders<OrderBookEntry>.Filter.Eq(
                        e => e.CreatedOnVersionId, matchOrder.TargetOrderOnVersionNumber),
                    Builders<OrderBookEntry>.Update.Set(
                        e => e.FilledQty, targetOrder.Qty - matchOrder.TargetOrderQtyRemaining)
                );
            }

            TransactionHistory.InsertMany(
                new[]
                {
                    new TransactionHistoryEntry
                    {
                        ExecutionTime = now,
                        User = matchOrder.ActionUser,
                        AccountId = matchOrder.ActionAccountId,
                        Instrument = matchOrder.Instrument,
                        Side = targetOrder.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                        OrderId = actionOrder?.Id,
                        // The entire quantity was filled
                        FilledQty = matchOrder.Qty,
                        Price = targetOrder.LimitPrice,
                    },
                    new TransactionHistoryEntry
                    {
                        ExecutionTime = now,
                        User = targetOrder.User,
                        AccountId = targetOrder.AccountId,
                        Instrument = targetOrder.Instrument,
                        Side = targetOrder.Side,
                        OrderId = targetOrder.Id,
                        // The entire quantity was filled
                        FilledQty = matchOrder.Qty,
                        Price = targetOrder.LimitPrice,
                    }
                }
            );

            _logger.LogDebug("Persisted match order @ version number " + matchOrder.VersionNumber);
        }

        private void AssertMatchOrderQty(
            MatchOrderEventEntry matchOrder, OrderBookEntry actionOrder, OrderBookEntry targetOrder)
        {
            if (actionOrder != null && matchOrder.ActionOrderQtyRemaining !=
                actionOrder.Qty - (actionOrder.FilledQty + matchOrder.Qty))
            {
                throw new Exception(
                    $"Integrity assertion failed! {nameof(MatchOrderEventEntry)} ID {matchOrder.Id} attempted to increase {nameof(targetOrder.FilledQty)} of action order ID {actionOrder.Id} from {actionOrder.FilledQty.ToString(CultureInfo.CurrentCulture)} by {matchOrder.Qty}, but that didn't add up to event entry-asserted value of {matchOrder.ActionOrderQtyRemaining.ToString(CultureInfo.CurrentCulture)}!");
            }

            if (matchOrder.TargetOrderQtyRemaining != targetOrder.Qty - (targetOrder.FilledQty + matchOrder.Qty))
            {
                throw new Exception(
                    $"Integrity assertion failed! {nameof(MatchOrderEventEntry)} ID {matchOrder.Id} attempted to increase {nameof(targetOrder.FilledQty)} of target order ID {targetOrder.Id} from {targetOrder.FilledQty.ToString(CultureInfo.CurrentCulture)} by {matchOrder.Qty}, but that didn't add up to event entry-asserted value of {matchOrder.TargetOrderQtyRemaining.ToString(CultureInfo.CurrentCulture)}!");
            }
        }

        /// <summary>
        /// Cancels an active order, inserting a relevant order history entry.
        /// </summary>
        /// <param name="cancelOrderCreatedOnVersionNumber">Version number of creation of the order to be canceled</param>
        /// <param name="orderHistoryTime">Time of the cancellation event</param>
        /// <param name="openOrder">Found open order</param>
        /// <returns>Remaining unmatched order amount of paid coin depending on side</returns>
        internal decimal CancelOrderByCreatedOnVersionNumber(
            long cancelOrderCreatedOnVersionNumber,
            DateTime orderHistoryTime,
            out object openOrder)
        {
            decimal remainingUnmatchedAmount;
            openOrder = FindOpenOrderCreatedByVersionNumber(cancelOrderCreatedOnVersionNumber);
            if (openOrder is OrderBookEntry limitOrder)
            {
                InsertOrderHistoryEntry(limitOrder.FilledQty, limitOrder, OrderStatus.Cancelled, orderHistoryTime);
                remainingUnmatchedAmount = limitOrder.Qty - limitOrder.FilledQty;
                if (limitOrder.Side == OrderSide.Buy)
                {
                    remainingUnmatchedAmount *= limitOrder.LimitPrice;
                }

                OrderBook.DeleteOne(
                    Builders<OrderBookEntry>.Filter.Eq(e => e.Id, limitOrder.Id)
                );
            }
            else if (openOrder is HiddenOrderEntry stopOrder)
            {
                InsertOrderHistoryEntry(stopOrder, OrderStatus.Cancelled, orderHistoryTime);
                remainingUnmatchedAmount = stopOrder.Qty;
                if (stopOrder.Side == OrderSide.Buy)
                {
                    remainingUnmatchedAmount *= stopOrder.StopPrice;
                }

                HiddenOrders.DeleteOne(
                    Builders<HiddenOrderEntry>.Filter.Eq(e => e.Id, stopOrder.Id)
                );
            }
            else
            {
                throw new Exception(
                    $"Couldn't cancel order created on version number {cancelOrderCreatedOnVersionNumber}, as there was no such order open");
            }

            return remainingUnmatchedAmount;
        }

        public object FindOpenOrderCreatedByVersionNumber(long cancelOrderCreatedOnVersionNumber)
        {
            var targetOrder = OrderBook.Find(
                Builders<OrderBookEntry>.Filter.Eq(e => e.CreatedOnVersionId, cancelOrderCreatedOnVersionNumber)
            ).SingleOrDefault();
            if (targetOrder != null)
            {
                return targetOrder;
            }
            else
            {
                // There has to be one or the other
                var hiddenOrder = HiddenOrders.Find(
                    Builders<HiddenOrderEntry>.Filter.Eq(e => e.CreatedOnVersionId, cancelOrderCreatedOnVersionNumber)
                ).Single();
                return hiddenOrder;
            }
        }

        private void InsertOrderHistoryEntry(
            decimal filledQty, OrderBookEntry orderToClose, OrderStatus status, DateTime closeTime)
        {
            OrderHistory.InsertOne(
                new OrderHistoryEntry
                {
                    CreateTime = orderToClose.EntryTime,
                    CloseTime = closeTime,
                    User = orderToClose.User,
                    AccountId = orderToClose.AccountId,
                    Instrument = orderToClose.Instrument,
                    Qty = orderToClose.Qty,
                    Side = orderToClose.Side,
                    // Closed limit order
                    Type = OrderType.Limit,
                    // The entire order quantity was filled
                    FilledQty = filledQty,
                    LimitPrice = orderToClose.LimitPrice,
                    StopPrice = null,
                    // TODO from stop loss and take profit
                    //ChildrenIds
                    DurationType = orderToClose.DurationType,
                    Duration = orderToClose.Duration,
                    Status = status,
                }
            );
        }

        private void InsertOrderHistoryEntry(
            HiddenOrderEntry orderToClose, OrderStatus status, DateTime closeTime)
        {
            OrderHistory.InsertOne(
                new OrderHistoryEntry
                {
                    CreateTime = orderToClose.EntryTime,
                    CloseTime = closeTime,
                    User = orderToClose.User,
                    AccountId = orderToClose.AccountId,
                    Instrument = orderToClose.Instrument,
                    Qty = orderToClose.Qty,
                    Side = orderToClose.Side,
                    // Closed stop order
                    Type = OrderType.Stop,
                    // Stop order does not fill quantity, it only creates a limit order
                    FilledQty = 0,
                    LimitPrice = null,
                    StopPrice = orderToClose.StopPrice,
                    // TODO from stop loss and take profit
                    //ChildrenIds
                    DurationType = orderToClose.DurationType,
                    Duration = orderToClose.Duration,
                    Status = status,
                }
            );
        }

        internal async Task<IAsyncCursor<OrderBookEntry>> MatchSellers(decimal? belowOrAt, string instrument)
        {
            return await
                (belowOrAt.HasValue
                    ? OrderBook.Find(e =>
                        e.Side == OrderSide.Sell && e.LimitPrice <= belowOrAt.Value && e.Instrument.Equals(instrument))
                    : OrderBook.Find(e =>
                        e.Side == OrderSide.Sell && e.Instrument.Equals(instrument))
                ).Sort(Builders<OrderBookEntry>.Sort.Ascending(e => e.LimitPrice))
                .ToCursorAsync();
        }

        internal async Task<IAsyncCursor<OrderBookEntry>> MatchBuyers(decimal? aboveOrAt, string instrument)
        {
            return await
                (aboveOrAt.HasValue
                    ? OrderBook.Find(e =>
                        e.Side == OrderSide.Buy && e.LimitPrice >= aboveOrAt.Value && e.Instrument.Equals(instrument))
                    : OrderBook.Find(e =>
                        e.Side == OrderSide.Buy && e.Instrument.Equals(instrument))
                ).Sort(Builders<OrderBookEntry>.Sort.Descending(e => e.LimitPrice))
                .ToCursorAsync();
        }
    }
}
