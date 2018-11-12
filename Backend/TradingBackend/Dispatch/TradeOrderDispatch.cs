using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Processors;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend.Dispatch
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
        internal Task Dispatch(Message message, Func<string, Task> reportInvalidMessage)
        {
            var user = (string)message.UserProperties[ParameterNames.User];
            var accountId = (string)message.UserProperties[ParameterNames.AccountId];
            var instrument = (string)message.UserProperties[ParameterNames.Instrument];
            var quantity = (decimal)message.UserProperties[ParameterNames.Quantity];
            var side = (string)message.UserProperties[ParameterNames.Side];
            var type = (string)message.UserProperties[ParameterNames.Type];
            var limitPrice = (decimal?)message.UserProperties[ParameterNames.LimitPrice];
            var stopPrice = (decimal?)message.UserProperties[ParameterNames.StopPrice];
            var durationType = (string)message.UserProperties[ParameterNames.DurationType];
            var duration = (decimal?)message.UserProperties[ParameterNames.Duration];
            var stopLoss = (decimal?)message.UserProperties[ParameterNames.StopLoss];
            var takeProfit = (decimal?)message.UserProperties[ParameterNames.TakeProfit];
            var requestId = (string)message.UserProperties[ParameterNames.RequestId];

            // Ignored request ID, maybe persist it to make sure no duplicates occur

            return ProcessorFactory.CreateTradeOrderPersistenceProcessor().PersistOrder(
                user, accountId, instrument, quantity, side, type, limitPrice, stopPrice, durationType, duration, stopLoss, takeProfit, reportInvalidMessage);
        }
    }
}
