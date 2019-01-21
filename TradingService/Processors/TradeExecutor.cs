using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.TradingService.Services;

namespace XchangeCrypt.Backend.TradingService.Processors
{
    /// <summary>
    /// Matches orders.
    /// </summary>
    public class TradeExecutor
    {
        public ActivityHistoryService ActivityHistoryService { get; }
        public LimitOrderService LimitOrderService { get; }
        public StopOrderService StopOrderService { get; }
        public MarketOrderService MarketOrderService { get; }

        public TradeExecutor(
            ActivityHistoryService activityHistoryService,
            LimitOrderService limitOrderService,
            StopOrderService stopOrderService,
            MarketOrderService marketOrderService)
        {
            ActivityHistoryService = activityHistoryService;
            LimitOrderService = limitOrderService;
            StopOrderService = stopOrderService;
            MarketOrderService = marketOrderService;
        }

        internal async Task Limit(Task<ActivityHistoryOrderEntry> task)
        {
            // TODO: refactor so that Service layer only directly uses Repositories
            var activities = ActivityHistoryService.ActivityHistoryRepository.Orders();
            var orderBook = LimitOrderService.TradingRepository.OrderBook();

            var activityEntry = await task;
            var limitOrder = await LimitOrderService.Insert(activityEntry);
            if (activityEntry.Side == OrderSide.Buy)
            {
                List<OrderBookEntry> sellers = await LimitOrderService.MatchSellers(limitOrder.LimitPrice.Value);
                Console.WriteLine($"Limit order matched {sellers.Count} sellers");
                while (sellers.Count > 0)
                {
                    // TODO sort
                    // TODO match
                    var seller = sellers[0];
                    var sellerOffer = seller.LimitPrice.Value - seller.FilledQty;
                    var orderRemaining = limitOrder.LimitPrice.Value - limitOrder.FilledQty;
                    if (sellerOffer >= orderRemaining)
                    {
                        // Entire order is consumed by the seller offer
                        MatchLimit(limitOrder, seller, orderRemaining);
                        // TODO remove?
                        limitOrder.Status = OrderStatus.Filled;
                        orderBook.ReplaceOne(
                            Builders<OrderBookEntry>.Filter.Where(e => e.Id.Equals(limitOrder.Id)),
                            limitOrder);
                    }
                    else if (sellerOffer < orderRemaining)
                    {
                        // Fraction of order will remain, but the seller offer will be consumed
                        MatchLimit(limitOrder, seller, sellerOffer);
                        // TODO remove?
                        seller.Status = OrderStatus.Filled;
                        orderBook.ReplaceOne(
                            Builders<OrderBookEntry>.Filter.Where(e => e.Id.Equals(seller.Id)),
                            seller);
                    }

                    sellers = await LimitOrderService.MatchSellers(limitOrder.LimitPrice.Value);
                }
            }
            else if (activityEntry.Side == OrderSide.Sell)
            {
                var buyers = await LimitOrderService.MatchBuyers(limitOrder.LimitPrice.Value);
                Console.WriteLine($"Limit order matched {buyers.Count} buyers");
                // TODO match
            }
        }

        private void MatchLimit(OrderBookEntry limitOrder, OrderBookEntry offer, decimal quantity)
        {
            // TODO in transaction
            limitOrder.FilledQty += quantity;
            offer.FilledQty += quantity;
            LimitOrderService.TradingRepository.OrderBook().ReplaceOne(
                Builders<OrderBookEntry>.Filter.Where(e => e.Id.Equals(limitOrder.Id)),
                limitOrder);
            LimitOrderService.TradingRepository.OrderBook().ReplaceOne(
                Builders<OrderBookEntry>.Filter.Where(e => e.Id.Equals(offer.Id)),
                offer);
            var orderTransaction = new TransactionHistoryEntry
            {
                EntryTime = DateTime.Now,
                User = limitOrder.User,
                AccountId = limitOrder.AccountId,
                Instrument = limitOrder.Instrument,
                Side = limitOrder.Side,
                OrderId = limitOrder.Id,
                FilledQty = TransformQuantityBySide(limitOrder.Side, quantity),
                Price = limitOrder.LimitPrice.Value,
            };
            var offerTransaction = new TransactionHistoryEntry
            {
                EntryTime = DateTime.Now,
                User = offer.User,
                AccountId = offer.AccountId,
                Instrument = offer.Instrument,
                Side = offer.Side,
                OrderId = offer.Id,
                FilledQty = TransformQuantityBySide(offer.Side, quantity),
                Price = offer.LimitPrice.Value,
            };
            var txCollection = LimitOrderService.TradingRepository.TransactionHistory();
            txCollection.InsertOne(orderTransaction);
            txCollection.InsertOne(offerTransaction);
        }

        private decimal TransformQuantityBySide(OrderSide orderSide, decimal quantity)
        {
            switch (orderSide)
            {
                case OrderSide.Buy:
                    return quantity;

                case OrderSide.Sell:
                    return -quantity;

                default:
                    throw new Exception("Unknown OrderSide");
            }
        }

        internal async Task Stop(Task<ActivityHistoryOrderEntry> task)
        {
            throw new NotImplementedException();
        }

        internal async Task Market(Task<ActivityHistoryOrderEntry> task)
        {
            throw new NotImplementedException();
        }
    }
}
