using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Models;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;
using XchangeCrypt.Backend.TradingBackend.Repositories;

namespace XchangeCrypt.Backend.TradingBackend.Services
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
            return OrderBook.Find(e => e.Side == OrderSide.Sell && e.Status == OrderStatus.Working && e.LimitPrice <= below).ToListAsync();
        }

        internal Task<List<OrderBookEntry>> MatchBuyers(decimal above)
        {
            return OrderBook.Find(e => e.Side == OrderSide.Sell && e.Status == OrderStatus.Working && e.LimitPrice >= above).ToListAsync();
        }
    }
}
