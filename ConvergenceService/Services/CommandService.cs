using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConvergenceService.Services.Hosted;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.ConvergenceService.Services
{
    /// <summary>
    /// Supports limit, stop, and market order operations.
    /// Also manages user's personal information.
    /// Uses a queue to send the requests to a trading backend or a wallet backend.
    /// </summary>
    public class CommandService
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private readonly TradingQueueWriter _tradingQueueWriter;
        private readonly WalletQueueWriter _walletQueueWriter;
        private readonly AnswerQueueReceiver _answerQueueReceiver;

        /// <summary>
        /// </summary>
        public CommandService(
            TradingQueueWriter tradingQueueWriter,
            WalletQueueWriter walletQueueWriter,
            AnswerQueueReceiver answerQueueReceiver)
        {
            _tradingQueueWriter = tradingQueueWriter;
            _walletQueueWriter = walletQueueWriter;
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

        /// <summary>
        /// Deletes an order, identifying it's creation version number as an orderId.
        /// </summary>
        /// <returns>null on success, otherwise the error message</returns>
        public async Task<string> CancelOrder(
            string user, string accountId, long orderCreatedOnVersionId, string requestId)
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

                        {ParameterNames.OrderType, OrderTypes.Cancel},
                        {ParameterNames.OrderCreatedOnVersionNumber, orderCreatedOnVersionId},
                        {ParameterNames.RequestId, requestId},
                        {ParameterNames.AnswerQueuePostfix, _answerQueueReceiver.QueryNamePostfix},
                    }
                );
            });
        }

        /// <summary>
        /// Generates a new wallet public key address.
        /// </summary>
        /// <returns>null on success, otherwise the error message</returns>
        public async Task<string> GenerateWallet(
            string user,
            string accountId,
            string coinSymbol,
            bool firstGeneration,
            string requestId)
        {
            requestId = Sha256Hash(requestId);
            return await ExecuteForAnswer(user, requestId, async () =>
            {
                await _walletQueueWriter.SendMessageAsync(
                    new Dictionary<string, object>
                    {
                        {ParameterNames.MessageType, MessageTypes.WalletOperation},
                        {ParameterNames.WalletCommandType, WalletCommandTypes.Generate},
                        {ParameterNames.User, user},
                        {ParameterNames.AccountId, accountId},
                        {ParameterNames.CoinSymbol, coinSymbol},
                        {ParameterNames.FirstGeneration, firstGeneration},
                        {ParameterNames.RequestId, requestId},
                        {ParameterNames.AnswerQueuePostfix, _answerQueueReceiver.QueryNamePostfix},
                    }
                );
            });
        }

        public async Task<string> WalletWithdraw(
            string user,
            string accountId,
            string coinSymbol,
            string recipientPublicKey,
            decimal withdrawalAmount,
            string requestId)
        {
            requestId = Sha256Hash(requestId);
            return await ExecuteForAnswer(user, requestId, async () =>
            {
                await _walletQueueWriter.SendMessageAsync(
                    new Dictionary<string, object>
                    {
                        {ParameterNames.MessageType, MessageTypes.WalletOperation},
                        {ParameterNames.WalletCommandType, WalletCommandTypes.Withdrawal},
                        {ParameterNames.User, user},
                        {ParameterNames.AccountId, accountId},
                        {ParameterNames.CoinSymbol, coinSymbol},
                        {ParameterNames.WithdrawalTargetPublicKey, recipientPublicKey},
                        {ParameterNames.Amount, withdrawalAmount},
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

        public static string RandomRequestId()
        {
            return new Random().Next(100_000_000).ToString();
        }
    }
}
