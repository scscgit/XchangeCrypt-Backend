using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XchangeCrypt.Backend.ConvergenceService.Services.Hosted;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.ConvergenceService.Services
{
    /// <summary>
    /// Supports limit, stop, and market order operations.
    /// Uses a queue to send the requests to a trading backend.
    /// </summary>
    public class OrderService
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private readonly TradingQueueWriter _tradingQueueWriter;
        private readonly AnswerQueueReceiver _answerQueueReceiver;

        /// <summary>
        /// </summary>
        public OrderService(
            TradingQueueWriter tradingQueueWriter,
            AnswerQueueReceiver answerQueueReceiver)
        {
            _tradingQueueWriter = tradingQueueWriter;
            _answerQueueReceiver = answerQueueReceiver;
        }


        /// <summary>
        /// Enqueues a Limit order.
        /// </summary>
        /// <returns>null on success, otherwise the error message</returns>
        public async Task<string> CreateLimitOrder(
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
            requestId = Sha256Hash(requestId);
            return await ExecuteForAnswer(user, requestId, async () =>
            {
                await _tradingQueueWriter.SendMessageAsync(
                    new Dictionary<string, object>
                    {
                        {ParameterNames.MessageType, MessageTypes.TradeOrder},
                        {ParameterNames.User, user},
                        {ParameterNames.AccountId, accountId},
                        {ParameterNames.Instrument, instrument},

                        {ParameterNames.Quantity, qty},
                        {ParameterNames.Side, side},
                        {ParameterNames.OrderType, OrderTypes.LimitOrder},
                        {ParameterNames.LimitPrice, limitPrice},
                        {ParameterNames.DurationType, durationType},
                        {ParameterNames.Duration, durationDateTime},
                        {ParameterNames.StopLoss, stopLoss},
                        {ParameterNames.TakeProfit, takeProfit},
                        {ParameterNames.RequestId, requestId},
                        {ParameterNames.AnswerQueuePostfix, _answerQueueReceiver.QueryNamePostfix},
                    }
                );
            });
        }

        /// <summary>
        /// Enqueues a Stop order.
        /// </summary>
        /// <returns>null on success, otherwise the error message</returns>
        public async Task<string> CreateStopOrder(
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
            requestId = Sha256Hash(requestId);
            return await ExecuteForAnswer(user, requestId, async () =>
            {
                await _tradingQueueWriter.SendMessageAsync(
                    new Dictionary<string, object>
                    {
                        {ParameterNames.MessageType, MessageTypes.TradeOrder},
                        {ParameterNames.User, user},
                        {ParameterNames.AccountId, accountId},
                        {ParameterNames.Instrument, instrument},

                        {ParameterNames.Quantity, qty},
                        {ParameterNames.Side, side},
                        {ParameterNames.OrderType, OrderTypes.StopOrder},
                        {ParameterNames.StopPrice, stopPrice},
                        {ParameterNames.DurationType, durationType},
                        {ParameterNames.Duration, durationDateTime},
                        {ParameterNames.StopLoss, stopLoss},
                        {ParameterNames.TakeProfit, takeProfit},
                        {ParameterNames.RequestId, requestId},
                        {ParameterNames.AnswerQueuePostfix, _answerQueueReceiver.QueryNamePostfix},
                    }
                );
            });
        }

        /// <summary>
        /// Enqueues a Market order.
        /// </summary>
        /// <returns>null on success, otherwise the error message</returns>
        public async Task<string> CreateMarketOrder(
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
            requestId = Sha256Hash(requestId);
            return await ExecuteForAnswer(user, requestId, async () =>
            {
                await _tradingQueueWriter.SendMessageAsync(
                    new Dictionary<string, object>
                    {
                        {ParameterNames.MessageType, MessageTypes.TradeOrder},
                        {ParameterNames.User, user},
                        {ParameterNames.AccountId, accountId},
                        {ParameterNames.Instrument, instrument},

                        {ParameterNames.Quantity, qty},
                        {ParameterNames.Side, side},
                        {ParameterNames.OrderType, OrderTypes.MarketOrder},
                        {ParameterNames.DurationType, durationType},
                        {ParameterNames.Duration, durationDateTime},
                        {ParameterNames.StopLoss, stopLoss},
                        {ParameterNames.TakeProfit, takeProfit},
                        {ParameterNames.RequestId, requestId},
                        {ParameterNames.AnswerQueuePostfix, _answerQueueReceiver.QueryNamePostfix},
                    }
                );
            });
        }

        private async Task<string> ExecuteForAnswer(string user, string requestId, Action queueAction)
        {
            _answerQueueReceiver.ExpectAnswer(user, requestId);
            string errorIfAny;
            try
            {
                queueAction();
            }
            finally
            {
                var message = await _answerQueueReceiver.WaitForAnswer(user, requestId, _timeout);
                errorIfAny = (string) message[ParameterNames.ErrorIfAny];
            }

            return errorIfAny;
        }

        private static string Sha256Hash(string input)
        {
            // Create a SHA256
            using (var sha256Hash = SHA256.Create())
            {
                // Convert byte array to a string
                var builder = new StringBuilder();
                foreach (var singleByte in sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input)))
                {
                    builder.Append(singleByte.ToString("X2"));
                }

                return builder.ToString();
            }
        }
    }
}
