using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;
using XchangeCrypt.Backend.TradingBackend.Services;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend.Processors
{
    public class TradeOrderPersistenceProcessor
    {
        public ActivityHistoryService ActivityHistoryService { get; }
        public TradeExecutor TradeExecutor { get; }

        /// <summary>
        /// Created via ProcessorFactory.
        /// </summary>
        public TradeOrderPersistenceProcessor(ActivityHistoryService activityHistoryService, TradeExecutor tradeExecutor)
        {
            ActivityHistoryService = activityHistoryService;
            TradeExecutor = tradeExecutor;
        }

        public Task PersistOrder(string user, string accountId, string instrument, decimal? quantity, string side, string type, decimal? limitPrice, decimal? stopPrice, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit, Func<string, Task> reportInvalidMessage)
        {
            var orderSideOptional = ParseSide(side);
            if (orderSideOptional.HasValue)
            {
                return reportInvalidMessage($"Unrecognized order side {side}");
            }
            var orderSide = orderSideOptional.Value;

            switch (type)
            {
                //todo
                case OrderTypes.LimitOrder:
                    return ActivityHistoryService.PersistLimitOrder(
                        user, accountId, instrument, quantity, orderSide, limitPrice, durationType, duration, stopLoss, takeProfit);

                case OrderTypes.StopOrder:
                    return ActivityHistoryService.PersistStopOrder(
                        user, accountId, instrument, quantity, orderSide, stopPrice, durationType, duration, stopLoss, takeProfit);

                case OrderTypes.MarketOrder:
                    return ActivityHistoryService.PersistMarketOrder(
                        user, accountId, instrument, quantity, orderSide, durationType, duration, stopLoss, takeProfit);

                default:
                    return reportInvalidMessage($"Unrecognized order type {type}");
            }
        }

        private OrderSide? ParseSide(string side)
        {
            switch (side)
            {
                case OrderSides.BuySide:
                    return OrderSide.Buy;

                case OrderSides.SellSide:
                    return OrderSide.Sell;

                default:
                    return null;
            }
        }
    }
}
