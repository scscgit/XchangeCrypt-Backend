using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.TradingService.Services
{
    // TODO: one instance per instrument?
    public class LimitOrderService : AbstractTradingOrderService
    {
        public TradingRepository TradingRepository { get; }
        public IMongoCollection<OrderBookEntry> OrderBook { get; }

        /// <summary>
        /// </summary>
        public LimitOrderService(TradingRepository tradingRepository)
        {
            TradingRepository = tradingRepository;
            OrderBook = tradingRepository.OrderBook();
        }

        internal async Task<OrderBookEntry> Insert(ActivityHistoryOrderEntry activityEntry)
        {
            var entry = new OrderBookEntry
            {
                EntryTime = activityEntry.EntryTime,
                User = activityEntry.User,
                AccountId = activityEntry.AccountId,
                Instrument = activityEntry.Instrument,
                Qty = activityEntry.Qty,
                Side = activityEntry.Side,
                FilledQty = 0m,
                LimitPrice = activityEntry.LimitPrice,
                // TODO from stop loss and take profit
                //ChildrenIds
                DurationType = activityEntry.DurationType,
                Duration = activityEntry.Duration,
                Status = OrderStatus.Working,
            };
            await TradingRepository.OrderBook().InsertOneAsync(entry);
            return entry;
        }

        internal Task<List<OrderBookEntry>> MatchSellers(decimal below)
        {
            return OrderBook
                .Find(e => e.Side == OrderSide.Sell && e.Status == OrderStatus.Working && e.LimitPrice <= below)
                // TODO: verify
                .Sort(Builders<OrderBookEntry>.Sort.Descending(e => e.LimitPrice))
                .ToListAsync();
        }

        internal Task<List<OrderBookEntry>> MatchBuyers(decimal above)
        {
            return OrderBook
                .Find(e => e.Side == OrderSide.Buy && e.Status == OrderStatus.Working && e.LimitPrice >= above)
                // TODO: verify
                .Sort(Builders<OrderBookEntry>.Sort.Ascending(e => e.LimitPrice))
                .ToListAsync();
        }
    }
}
