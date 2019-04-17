using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XchangeCrypt.Backend.ConstantsLibrary.Extensions;
using XchangeCrypt.Backend.TradingService.Processors;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingService.Dispatch
{
    public class TradeOrderDispatch
    {
        public ProcessorFactory ProcessorFactory { get; }

        public TradeOrderDispatch(ProcessorFactory processorFactory)
        {
            ProcessorFactory = processorFactory;
        }

        /// <summary>
        /// Dispatches proper handlers to handle a trade order operation.
        /// Tries to schedule persistence operations so that they execute in parallel as much as possible.
        /// </summary>
        /// <param name="message">Message to be processed</param>
        /// <param name="reportInvalidMessage">Error handler to call if the intended handler experienced error. Parameter is error message</param>
        internal Task Dispatch(IDictionary<string, object> message, Func<string, Exception> reportInvalidMessage)
        {
            var user = (string) message.GetValueOrDefault(ParameterNames.User);
            var accountId = (string) message.GetValueOrDefault(ParameterNames.AccountId);
            var instrument = (string) message.GetValueOrDefault(ParameterNames.Instrument);
            var quantity = (decimal?) message.GetValueOrDefault(ParameterNames.Quantity);
            var side = (string) message.GetValueOrDefault(ParameterNames.Side);
            var orderType = (string) message.GetValueOrDefault(ParameterNames.OrderType);
            var limitPrice = (decimal?) message.GetValueOrDefault(ParameterNames.LimitPrice);
            var stopPrice = (decimal?) message.GetValueOrDefault(ParameterNames.StopPrice);
            var durationType = (string) message.GetValueOrDefault(ParameterNames.DurationType);
            var duration = (decimal?) message.GetValueOrDefault(ParameterNames.Duration);
            var stopLoss = (decimal?) message.GetValueOrDefault(ParameterNames.StopLoss);
            var takeProfit = (decimal?) message.GetValueOrDefault(ParameterNames.TakeProfit);
            var orderCreatedOnVersionNumber =
                (long?) message.GetValueOrDefault(ParameterNames.OrderCreatedOnVersionNumber);
            var requestId = (string) message.GetValueOrDefault(ParameterNames.RequestId);

            // Ignored request ID, maybe persist it to make sure no duplicates occur

            return ProcessorFactory.CreateTradeOrderPersistenceProcessor().ExecuteTradeOrderCommand(
                user, accountId, instrument, quantity, side, orderType, limitPrice, stopPrice, durationType, duration,
                stopLoss, takeProfit, orderCreatedOnVersionNumber, requestId, reportInvalidMessage);
        }
    }
}
