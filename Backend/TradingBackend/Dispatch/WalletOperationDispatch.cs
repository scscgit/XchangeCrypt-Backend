using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Processors;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend.Dispatch
{
    public class WalletOperationDispatch
    {
        public ProcessorFactory ProcessorFactory { get; }

        public WalletOperationDispatch(ProcessorFactory processorFactory)
        {
            ProcessorFactory = processorFactory;
        }

        /// <summary>
        /// Dispatches proper handlers to handle a wallet operation.
        /// Tries to schedule persistence operations so that they execute in parallel as much as possible.
        /// </summary>
        /// <param name="message">Message to be processed</param>
        /// <param name="reportInvalidMessage">Error handler to call if the intended handler experienced error. Parameter is error message</param>
        internal Task Dispatch(Message message, Func<string, Task> reportInvalidMessage)
        {
            var user = (string)message.UserProperties[ParameterNames.User];
            var accountId = (string)message.UserProperties[ParameterNames.AccountId];
            var coinSymbol = (string)message.UserProperties[ParameterNames.CoinSymbol];
            var depositType = (string)message.UserProperties[ParameterNames.DepositType];
            var withdrawalType = (string)message.UserProperties[ParameterNames.WithdrawalType];
            var amount = (decimal)message.UserProperties[ParameterNames.Amount];
            var requestId = (string)message.UserProperties[ParameterNames.RequestId];

            // Ignored request ID, maybe persist it to make sure no duplicates occur

            //todo
            return ProcessorFactory.CreateWalletOperationPersistenceProcessor().PersistWalletOperation(
                user, accountId, coinSymbol, depositType, withdrawalType, amount, reportInvalidMessage);
        }
    }
}
