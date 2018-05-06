using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Models;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;
using XchangeCrypt.Backend.TradingBackend.Services;

namespace XchangeCrypt.Backend.TradingBackend.Processors
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
                var sellers = await LimitOrderService.MatchSellers(limitOrder.LimitPrice.Value);
                Console.WriteLine($"Limit order matched {sellers.Count} sellers");
                // TODO match
            }
            else if (activityEntry.Side == OrderSide.Sell)
            {
                var buyers = await LimitOrderService.MatchBuyers(limitOrder.LimitPrice.Value);
                Console.WriteLine($"Limit order matched {buyers.Count} buyers");
                // TODO match
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
