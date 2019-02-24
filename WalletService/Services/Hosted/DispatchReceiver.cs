using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.QueueAccess;
using XchangeCrypt.Backend.WalletService.Dispatch;

namespace XchangeCrypt.Backend.WalletService.Services.Hosted
{
    public class DispatchReceiver : AbstractDispatchReceiver
    {
        private readonly WalletOperationDispatch _walletOperationDispatch;
        private readonly ILogger<DispatchReceiver> _logger;
        private readonly ILogger<QueueWriter> _queueWriterLogger;
        private readonly string _connectionString;
        private readonly string _answerQueueNamePrefix;

        public DispatchReceiver(
            WalletOperationDispatch walletOperationDispatch,
            IConfiguration configuration,
            ILogger<DispatchReceiver> logger,
            ILogger<QueueWriter> queueWriterLogger)
            : base(
                configuration["Queue:ConnectionString"] ?? throw new ArgumentException("Queue:ConnectionString"),
                configuration["Queue:Name"] ?? throw new ArgumentException("Queue:Name"),
                configuration["Queue:DeadLetter"] ?? throw new ArgumentException("Queue:DeadLetter"),
                Program.Shutdown,
                logger)
        {
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
            switch (message[MessagingConstants.ParameterNames.MessageType])
            {
                case MessagingConstants.MessageTypes.TradeOrder:
                    await ReportInvalidMessage(queueMessage,
                        $"Wrong queue for MessageType {message[MessagingConstants.ParameterNames.MessageType]}");
                    break;

                case MessagingConstants.MessageTypes.WalletOperation:
                    await _walletOperationDispatch.Dispatch(message,
                        errorMessage => throw ReportInvalidMessage(queueMessage, errorMessage).Result);
                    break;

                default:
                    await ReportInvalidMessage(queueMessage,
                        $"Unrecognized MessageType {message[MessagingConstants.ParameterNames.MessageType]}");
                    throw new Exception("This never occurs");
            }
        }
    }
}
