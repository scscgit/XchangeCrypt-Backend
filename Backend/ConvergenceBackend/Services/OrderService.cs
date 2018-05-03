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
        public OrderService(QueueWriter queueWriter)
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
           decimal? qty,
           string side,
           string type,
           decimal? limitPrice,
           string durationType,
           decimal? durationDateTime,
           decimal? stopLoss,
           decimal? takeProfit,
           string requestId)
        {
            await _queueWriter.SendMessageAsync(
                new Dictionary<string, object>()
                {
                    { ParameterNames.MessageType, MessageTypes.LimitOrder},
                    { ParameterNames.User, user},
                    { ParameterNames.AccountId, accountId},
                    { ParameterNames.Instrument, instrument},

                    { ParameterNames.Quantity, qty},
                    { ParameterNames.Side, side},
                    { ParameterNames.Type, type},
                    { ParameterNames.LimitPrice, limitPrice},
                    { ParameterNames.DurationType, durationType},
                    { ParameterNames.Duration, durationDateTime},
                    { ParameterNames.StopLoss, stopLoss},
                    { ParameterNames.TakeProfit, takeProfit},
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
           decimal? qty,
           string side,
           string type,
           decimal? stopPrice,
           string durationType,
           decimal? durationDateTime,
           decimal? stopLoss,
           decimal? takeProfit,
           string requestId)
        {
            await _queueWriter.SendMessageAsync(
                new Dictionary<string, object>()
                {
                    { ParameterNames.MessageType, MessageTypes.StopOrder},
                    { ParameterNames.User, user},
                    { ParameterNames.AccountId, accountId},
                    { ParameterNames.Instrument, instrument},

                    { ParameterNames.Quantity, qty},
                    { ParameterNames.Side, side},
                    { ParameterNames.Type, type},
                    { ParameterNames.StopPrice, stopPrice},
                    { ParameterNames.DurationType, durationType},
                    { ParameterNames.Duration, durationDateTime},
                    { ParameterNames.StopLoss, stopLoss},
                    { ParameterNames.TakeProfit, takeProfit},
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
           decimal? qty,
           string side,
           string type,
           string durationType,
           decimal? durationDateTime,
           decimal? stopLoss,
           decimal? takeProfit,
           string requestId)
        {
            await _queueWriter.SendMessageAsync(
                new Dictionary<string, object>()
                {
                    { ParameterNames.AccountId, accountId},
                    { ParameterNames.User, user},
                    { ParameterNames.MessageType, MessageTypes.MarketOrder},
                    { ParameterNames.AccountId, accountId},
                    { ParameterNames.Instrument, instrument},

                    { ParameterNames.Quantity, qty},
                    { ParameterNames.Side, side},
                    { ParameterNames.Type, type},
                    { ParameterNames.DurationType, durationType},
                    { ParameterNames.Duration, durationDateTime},
                    { ParameterNames.StopLoss, stopLoss},
                    { ParameterNames.TakeProfit, takeProfit},
                },
                null
            );
        }
    }
}
