using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using XchangeCrypt.Backend.TradingService.Services.Meta;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingService.Dispatch
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
            _logger.LogInformation($"Starting {typeof(DispatchReceiver).Name}");
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

            _logger.LogInformation($"Initialized {typeof(DispatchReceiver).Name}, listening for messages");
            while (!_stopped)
            {
                await ReceiveMessagesAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                _logger.LogDebug($"{typeof(DispatchReceiver).Name} still listening for messages...");
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
                    await ProcessMessagesAsync(queueMessage);
                    _logger.LogInformation($"Successfully consumed message {queueMessage.Id}");
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
                if (queueMessage.DequeueCount > 1)
                {
                    throw new Exception("Message was already dequeued, and thus the potential duplicate is invalid");
                }

                // Parse the message
                var message = ParseCloudMessage(queueMessage);

                // Log the message
                var messagePairs = string.Join(
                    Environment.NewLine, message.Select(pair => pair.Key + ": " + pair.Value.ToString())
                );
                var messageDescription =
                    $"Consuming message {message[ParameterNames.MessageType]} with ID {queueMessage.Id} {{\n{messagePairs}\n}}";
                _logger.LogInformation(messageDescription);
                _monitorService.LastMessage = messageDescription;

                // Process the message
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
                    $"Exception message: {e.Message}\nStackTrace:\n{e.StackTrace}");
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
                // Parse the old message to be added as a content of a dead letter message
                dictionary = ParseCloudMessage(queueMessage);
            }
            catch (Exception e)
            {
                // Report the parsing error instead
                _logger.LogError(
                    $"ErrorMessageFormat in message ID {queueMessage.Id}, caused by: {e.Message}");
                dictionary = new Dictionary<string, object> {{"ErrorMessageFormat", e.Message + "\n" + e.StackTrace}};
            }

            dictionary.Add("Id", queueMessage.Id);
            dictionary.Add("ErrorMessage", errorMessage);
            var deadLetterMessage = new CloudQueueMessage(JsonConvert.SerializeObject(dictionary));
            // Send the dead letter message
            await _queueDeadLetter.AddMessageAsync(deadLetterMessage);
            // Mark the message as completed, so that it is not received again
            await _queue.DeleteMessageAsync(queueMessage);

            throw new InvalidMessageException(
                $"Message handler couldn't handle a message with ID {queueMessage.Id}. "
                + $"Reported as a DeadLetter message ID {deadLetterMessage.Id}. Error was: {errorMessage}"
            );
        }

        private static IDictionary<string, object> ParseCloudMessage(CloudQueueMessage queueMessage)
        {
            return JsonConvert.DeserializeObject<IDictionary<string, object>>(
                queueMessage.AsString,
                new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                }
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
