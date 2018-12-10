using System;
using System.Collections.Generic;
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
        internal Task Dispatch(IDictionary<string, object> message, Func<string, Task> reportInvalidMessage)
        {
            var user = (string) message[ParameterNames.User];
            var accountId = (string) message[ParameterNames.AccountId];
            var coinSymbol = (string) message[ParameterNames.CoinSymbol];
            var depositType = (string) message[ParameterNames.DepositType];
            var withdrawalType = (string) message[ParameterNames.WithdrawalType];
            var amount = (decimal) message[ParameterNames.Amount];
            var requestId = (string) message[ParameterNames.RequestId];

            // Ignored request ID, maybe persist it to make sure no duplicates occur

            //todo
            return ProcessorFactory.CreateWalletOperationPersistenceProcessor().PersistWalletOperation(
                user, accountId, coinSymbol, depositType, withdrawalType, amount, reportInvalidMessage);
        }
    }
}
