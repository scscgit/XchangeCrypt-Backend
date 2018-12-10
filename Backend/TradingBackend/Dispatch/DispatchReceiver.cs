using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using XchangeCrypt.Backend.TradingBackend.Services;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend.Dispatch
{
    public class DispatchReceiver : IHostedService
    {
        private readonly ILogger<DispatchReceiver> _logger;
        private readonly IConfiguration _configuration;
        private readonly MonitorService _monitorService;
        private readonly TradeOrderDispatch _tradeOrderDispatch;
        private readonly WalletOperationDispatch _walletOperationDispatch;
        private CloudQueue _queue;
        private CloudQueue _queueDeadLetter;
        private bool _stopped;

        public DispatchReceiver(
            ILogger<DispatchReceiver> logger,
            IConfiguration configuration,
            MonitorService monitorService,
            TradeOrderDispatch tradeOrderDispatch,
            WalletOperationDispatch walletOperationDispatch
        )
        {
            _logger = logger;
            _configuration = configuration;
            _monitorService = monitorService;
            _tradeOrderDispatch = tradeOrderDispatch;
            _walletOperationDispatch = walletOperationDispatch;
        }

        /// <summary>
        /// Handler registration.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration["Queue:ConnectionString"]);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(_configuration["Queue:Name"]);
            if (await _queue.CreateIfNotExistsAsync())
            {
                _logger.LogInformation($"Created queue {_configuration["Queue:Name"]}");
            }

            _queueDeadLetter = queueClient.GetQueueReference(_configuration["Queue:DeadLetter"]);
            if (await _queueDeadLetter.CreateIfNotExistsAsync())
            {
                _logger.LogInformation($"Created queue {_configuration["Queue:DeadLetter"]}");
            }

            while (!_stopped)
            {
                await ReceiveMessagesAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stopped = true;
            return Task.CompletedTask;
        }

        private async Task ReceiveMessagesAsync()
        {
            foreach (var queueMessage in await _queue.GetMessagesAsync(32))
            {
                try
                {
                    var message = await ProcessMessagesAsync(queueMessage);
                    var messageDescription =
                        $"{message[ParameterNames.MessageType]} ID {queueMessage.Id}, body: {message["Body"]}";
                    _logger.LogInformation($"Consumed message {messageDescription}");
                    _monitorService.LastMessage = messageDescription;
                }
                catch (InvalidMessageException e)
                {
                    _logger.LogError(e.Message);
                    _monitorService.ReportError(e.Message);
                }
            }
        }

        private async Task<IDictionary<string, object>> ProcessMessagesAsync(CloudQueueMessage queueMessage)
        {
            try
            {
                if (queueMessage.DequeueCount > 0)
                {
                    throw new Exception("Message was already dequeued, and thus the potential duplicate is invalid");
                }

                var message = JsonConvert.DeserializeObject<IDictionary<string, object>>(queueMessage.AsString);

                // Process the message
                var messageBody = message["MessageBody"] ?? "";
                Console.WriteLine(
                    $"Received message: Id:{queueMessage.Id} Body:{messageBody}");

                Task dispatchedTask;
                switch (message[ParameterNames.MessageType])
                {
                    case MessageTypes.TradeOrder:
                        dispatchedTask = _tradeOrderDispatch.Dispatch(message,
                            errorMessage => ReportInvalidMessage(queueMessage, errorMessage));
                        break;

                    case MessageTypes.WalletOperation:
                        dispatchedTask = _walletOperationDispatch.Dispatch(message,
                            errorMessage => ReportInvalidMessage(queueMessage, errorMessage));
                        break;

                    default:
                        await ReportInvalidMessage(queueMessage,
                            $"Unrecognized MessageType {message[ParameterNames.MessageType]}");
                        throw new Exception("This never occurs");
                }

                // Throws invalid message exception on error
                await dispatchedTask;

                // Complete the message so that it is not received again.
                await _queue.DeleteMessageAsync(queueMessage);
                return message;
            }
            catch (InvalidMessageException)
            {
                // Invalid message exception isn't avoided, but it mustn't cause the report of yet another invalid message
                throw;
            }
            catch (Exception e)
            {
                await ReportInvalidMessage(
                    queueMessage,
                    $"{e.GetType().Name} was thrown inside {typeof(DispatchReceiver).Name} during message processing. " +
                    $"Exception message: {e.Message}. StackTrace:\n{e.StackTrace}");
                throw new Exception("This never occurs");
            }
        }

        private async Task ReportInvalidMessage(CloudQueueMessage queueMessage, string errorMessage)
        {
            if (queueMessage == null)
            {
                _logger.LogError("Attempted to report invalid message, which was actually null.");
                return;
            }

            IDictionary<string, object> dictionary;
            try
            {
                dictionary = JsonConvert.DeserializeObject<IDictionary<string, object>>(queueMessage.AsString);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"ErrorMessageFormat in message ID {queueMessage.Id}, caused by: {e.Message}");
                dictionary = new Dictionary<string, object> {{"ErrorMessageFormat", e.Message + "\n" + e.StackTrace}};
            }

            dictionary.Add("Id", queueMessage.Id);
            dictionary.Add("ErrorMessage", errorMessage);
            var deadLetterMessage = new CloudQueueMessage(JsonConvert.SerializeObject(dictionary));
            await _queueDeadLetter.AddMessageAsync(deadLetterMessage);
            // Complete the message so that it is not received again.
            await _queue.DeleteMessageAsync(queueMessage);

            throw new InvalidMessageException(
                $"Message handler couldn't handle a message with ID {queueMessage.Id}. "
                + $"Reported as a DeadLetter message ID {deadLetterMessage.Id}. Error was: {errorMessage}"
            );
        }

        private class InvalidMessageException : Exception
        {
            public InvalidMessageException(string message) : base(message)
            {
            }
        }
    }
}
