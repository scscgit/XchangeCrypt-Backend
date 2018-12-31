using System.Collections.Generic;
using System.Threading.Tasks;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.ConvergenceBackend.Services
{
    /// <summary>
    /// Supports limit, stop, and market order operations.
    /// Uses a queue to send the requests to a trading backend.
    /// </summary>
    public class OrderService
    {
        private readonly QueueWriter _queueWriter;

        /// <summary>
        /// </summary>
        public OrderService(TradingBackendQueueWriter queueWriter)
        {
            _queueWriter = queueWriter;
        }

        /// <summary>
        /// Enqueues a Limit order.
        /// </summary>
        public async Task CreateLimitOrder(
            string user,
            string accountId,
            string instrument,
            decimal qty,
            string side,
            decimal limitPrice,
            string durationType,
            decimal? durationDateTime,
            decimal? stopLoss,
            decimal? takeProfit,
            string requestId)
        {
            await _queueWriter.SendMessageAsync(
                new Dictionary<string, object>
                {
                    {ParameterNames.MessageType, MessageTypes.TradeOrder},
                    {ParameterNames.User, user},
                    {ParameterNames.AccountId, accountId},
                    {ParameterNames.Instrument, instrument},

                    {ParameterNames.Quantity, qty},
                    {ParameterNames.Side, side},
                    {ParameterNames.Type, OrderTypes.LimitOrder},
                    {ParameterNames.LimitPrice, limitPrice},
                    {ParameterNames.DurationType, durationType},
                    {ParameterNames.Duration, durationDateTime},
                    {ParameterNames.StopLoss, stopLoss},
                    {ParameterNames.TakeProfit, takeProfit},
                },
                null
            );
        }

        /// <summary>
        /// Enqueues a Stop order.
        /// </summary>
        public async Task CreateStopOrder(
            string user,
            string accountId,
            string instrument,
            decimal qty,
            string side,
            decimal stopPrice,
            string durationType,
            decimal? durationDateTime,
            decimal? stopLoss,
            decimal? takeProfit,
            string requestId)
        {
            await _queueWriter.SendMessageAsync(
                new Dictionary<string, object>
                {
                    {ParameterNames.MessageType, MessageTypes.TradeOrder},
                    {ParameterNames.User, user},
                    {ParameterNames.AccountId, accountId},
                    {ParameterNames.Instrument, instrument},

                    {ParameterNames.Quantity, qty},
                    {ParameterNames.Side, side},
                    {ParameterNames.Type, OrderTypes.StopOrder},
                    {ParameterNames.StopPrice, stopPrice},
                    {ParameterNames.DurationType, durationType},
                    {ParameterNames.Duration, durationDateTime},
                    {ParameterNames.StopLoss, stopLoss},
                    {ParameterNames.TakeProfit, takeProfit},
                },
                null
            );
        }

        /// <summary>
        /// Enqueues a Market order.
        /// </summary>
        public async Task CreateMarketOrder(
            string user,
            string accountId,
            string instrument,
            decimal qty,
            string side,
            string durationType,
            decimal? durationDateTime,
            decimal? stopLoss,
            decimal? takeProfit,
            string requestId)
        {
            await _queueWriter.SendMessageAsync(
                new Dictionary<string, object>
                {
                    {ParameterNames.MessageType, MessageTypes.TradeOrder},
                    {ParameterNames.User, user},
                    {ParameterNames.AccountId, accountId},
                    {ParameterNames.Instrument, instrument},

                    {ParameterNames.Quantity, qty},
                    {ParameterNames.Side, side},
                    {ParameterNames.Type, OrderTypes.MarketOrder},
                    {ParameterNames.DurationType, durationType},
                    {ParameterNames.Duration, durationDateTime},
                    {ParameterNames.StopLoss, stopLoss},
                    {ParameterNames.TakeProfit, takeProfit},
                },
                null
            );
        }
    }
}
