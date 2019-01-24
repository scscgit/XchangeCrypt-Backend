using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using XchangeCrypt.Backend.QueueAccess;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingService.Dispatch
{
    public class DispatchReceiver : AbstractDispatchReceiver
    {
        private readonly TradeOrderDispatch _tradeOrderDispatch;
        private readonly WalletOperationDispatch _walletOperationDispatch;
        private readonly ILogger<DispatchReceiver> _logger;
        private readonly ILogger<QueueWriter> _queueWriterLogger;
        private readonly string _connectionString;
        private readonly string _answerQueueNamePrefix;

        public DispatchReceiver(
            TradeOrderDispatch tradeOrderDispatch,
            WalletOperationDispatch walletOperationDispatch,
            IConfiguration configuration,
            ILogger<DispatchReceiver> logger,
            ILogger<QueueWriter> queueWriterLogger)
            : base(
                configuration["Queue:ConnectionString"],
                configuration["Queue:Name"],
                configuration["Queue:DeadLetter"],
                logger)
        {
            _tradeOrderDispatch = tradeOrderDispatch;
            _walletOperationDispatch = walletOperationDispatch;
            _logger = logger;
            _queueWriterLogger = queueWriterLogger;
            _connectionString = configuration["Queue:ConnectionString"]
                                ?? throw new ArgumentException("Queue:ConnectionString");
            _answerQueueNamePrefix = configuration["Queue:ConvergenceAnswerNamePrefix"]
                                     ?? throw new ArgumentException("Queue:ConvergenceAnswerNamePrefix");
        }

        protected override QueueWriter ConfigureQueueAnswerWriter(Func<string> answerQueuePostfixRequest)
        {
            return new QueueWriter(
                _connectionString,
                _answerQueueNamePrefix + answerQueuePostfixRequest(),
                _queueWriterLogger
            );
        }

        protected override async Task Dispatch(CloudQueueMessage queueMessage, IDictionary<string, object> message)
        {
            switch (message[ParameterNames.MessageType])
            {
                case MessageTypes.TradeOrder:
                    await _tradeOrderDispatch.Dispatch(message,
                        errorMessage => ReportInvalidMessage(queueMessage, errorMessage));
                    break;

                case MessageTypes.WalletOperation:
                    await _walletOperationDispatch.Dispatch(message,
                        errorMessage => ReportInvalidMessage(queueMessage, errorMessage));
                    break;

                default:
                    await ReportInvalidMessage(queueMessage,
                        $"Unrecognized MessageType {message[ParameterNames.MessageType]}");
                    throw new Exception("This never occurs");
            }
        }
    }
}
