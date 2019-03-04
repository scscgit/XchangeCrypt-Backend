using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConstantsLibrary.Extensions;
using XchangeCrypt.Backend.WalletService.Processors;

namespace XchangeCrypt.Backend.WalletService.Dispatch
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
        internal Task Dispatch(IDictionary<string, object> message, Func<string, Exception> reportInvalidMessage)
        {
            var user = (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.User);
            var accountId = (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.AccountId);
            var coinSymbol = (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.CoinSymbol);
            var walletCommandType =
                (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.WalletCommandType);
            var amount = (decimal?) message.GetValueOrDefault(MessagingConstants.ParameterNames.Amount);
            var walletEventIdReference =
                (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.WalletEventIdReference);
            var requestId = (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.RequestId);

            // Ignored request ID, maybe persist it to make sure no duplicates occur

            //todo
            return ProcessorFactory.CreateWalletOperationPersistenceProcessor().ExecuteWalletOperationCommand(
                user, accountId, coinSymbol, walletCommandType, amount, walletEventIdReference, requestId,
                reportInvalidMessage);
        }
    }
}
