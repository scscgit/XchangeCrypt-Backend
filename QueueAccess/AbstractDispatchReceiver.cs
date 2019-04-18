using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConstantsLibrary.Extensions;

namespace XchangeCrypt.Backend.QueueAccess
{
    public abstract class AbstractDispatchReceiver : BackgroundService
    {
        private const bool StrictInvalidMessageCrashPolicy = false;

        public string QueryNamePostfix { get; private set; }

        private readonly TimeSpan _listeningInterval = TimeSpan.FromMilliseconds(2000);
        private readonly ILogger<AbstractDispatchReceiver> _logger;
        private readonly string _connectionString;
        protected string _queueName;
        private readonly string _deadLetterQueueName;
        private readonly Action _shutdownAction;
        private CloudQueue _queue;
        private CloudQueue _deadLetterQueue;
        private bool _stopped;
        protected CloudQueueClient _queueClient;

        protected AbstractDispatchReceiver(
            string connectionString,
            string queueNamePrefix,
            string deadLetterQueueName,
            Action shutdownAction,
            ILogger<AbstractDispatchReceiver> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _queueName = queueNamePrefix ?? throw new ArgumentNullException(nameof(queueNamePrefix));
            _deadLetterQueueName = deadLetterQueueName ??
                                   throw new ArgumentNullException(nameof(deadLetterQueueName));
            _shutdownAction = shutdownAction;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation($"Starting {GetType().Name}");
                var storageAccount = CloudStorageAccount.Parse(_connectionString);
                _queueClient = storageAccount.CreateCloudQueueClient();

                // Stores the original queue names in case their postfix is changed later
                var queueNamePrefix = _queueName;
                //var deadLetterQueueNamePrefix = _deadLetterQueueName;

                // As long as the service is not stopped, provide a support for dispatcher reset, and listen on the queue
                do
                {
                    DispatcherPostfixReset(out var queryNamePostfix);
                    QueryNamePostfix = queryNamePostfix;
                    if (QueryNamePostfix != null)
                    {
                        _queueName = queueNamePrefix + QueryNamePostfix;
                        //_deadLetterQueueName = deadLetterQueueNamePrefix + QueryNamePostfix;
                    }

                    try
                    {
                        await ListenOnQueue(stoppingToken);
                    }
                    catch (DispatcherResetJump)
                    {
                    }
                }
                while (!_stopped);
            }
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}\n{e.StackTrace}");
                _shutdownAction();
                throw;
            }
        }

        protected virtual void DispatcherPostfixReset(out string queueNamePostfix)
        {
            // The reset is implemented as a noop by default
            queueNamePostfix = null;
        }

        private async Task ListenOnQueue(CancellationToken cancellationToken)
        {
            _queue = _queueClient.GetQueueReference(_queueName);
            if (await _queue.CreateIfNotExistsAsync())
            {
                _logger.LogWarning($"Created queue {_queueName}");
            }

            _deadLetterQueue = _queueClient.GetQueueReference(_deadLetterQueueName);
            if (await _deadLetterQueue.CreateIfNotExistsAsync())
            {
                _logger.LogWarning($"Created dead letter queue {_deadLetterQueueName}");
            }

            _logger.LogInformation(
                $"Initialized {GetType().Name}, listening for messages on queue \"{_queueName}\" and storing dead letters in \"{_deadLetterQueueName}\"");
            while (!_stopped)
            {
                ReceiveMessagesAsync().Wait(cancellationToken);
                // Don't pass the cancellationToken, because we don't want the receiver
                // to shutdown application from a simple testing API call timeout. // Edit: actually nevermind.
                await Task.Delay(_listeningInterval, cancellationToken);
                _logger.LogDebug($"{GetType().Name} is still listening for messages...");
            }
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public new async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopped = true;
            _logger.LogWarning("Stopping dispatch receiver");
            await base.StopAsync(cancellationToken);
        }

        private async Task ReceiveMessagesAsync()
        {
            foreach (var queueMessage in await _queue.GetMessagesAsync(32))
            {
                try
                {
                    await ProcessMessagesAsync(queueMessage);
                }
                catch (InvalidMessageException e)
                {
                    _logger.LogError(e.Message);
                    if (StrictInvalidMessageCrashPolicy)
                    {
                        throw;
                    }
                }

                _logger.LogInformation($"Finished consuming message ID {queueMessage.Id}");
            }
        }

        private async Task AfterProcessedMessage(IDictionary<string, object> message, string errorIfAny)
        {
            try
            {
                // Only give implementor the postfix if he really asks for it!
                // And when he does, assert to make sure it's really available. Empty string is valid too.
                var writer = ConfigureQueueAnswerWriter(
                    () => (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.AnswerQueuePostfix)
                          ?? throw new ArgumentNullException(MessagingConstants.ParameterNames.AnswerQueuePostfix)
                );
                if (writer != null)
                {
                    // Get the rest of the message answer header
                    var user = (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.User);
                    var requestId = (string) message.GetValueOrDefault(MessagingConstants.ParameterNames.RequestId);
                    if (user == null || requestId == null)
                    {
                        throw new Exception(
                            $"Unexpected null within required fields: user {user ?? "null"}, requestId {requestId ?? "null"}");
                    }

                    await writer.SendMessageAsync(new Dictionary<string, object>
                    {
                        {MessagingConstants.ParameterNames.User, user},
                        {MessagingConstants.ParameterNames.RequestId, requestId},
                        {MessagingConstants.ParameterNames.ErrorIfAny, errorIfAny},
                    });
                    _logger.LogInformation(
                        $"Successfully answered {(errorIfAny == null ? "a success result" : "an error: " + errorIfAny)} to requestId \"{requestId}\"");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"Failed to execute message post-processing (e.g. answer): {e.Message}\n{e.StackTrace}");
            }
        }

        protected abstract QueueWriter ConfigureQueueAnswerWriter(Func<string> answerQueuePostfixRequest);

        private async Task ProcessMessagesAsync(CloudQueueMessage queueMessage)
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
                _logger.LogInformation(
                    $"Started consuming message with ID {queueMessage.Id} {MessagePairsToString(message)}");

                try
                {
                    // Process the message and throw invalid message exception on error
                    try
                    {
                        await Dispatch(queueMessage, message);
                    }
                    catch (AggregateException e)
                    {
                        while (e.InnerException is AggregateException anotherAggregate && e.InnerExceptions.Count == 1)
                        {
                            e = anotherAggregate;
                        }

                        // Rethrow a possible InvalidMessageException
                        throw e.InnerException;
                    }
                }
                catch (InvalidMessageException e)
                {
                    // We can only reply after the header is successfully parsed
                    await AfterProcessedMessage(message, e.OriginalErrorMessage);
                    throw;
                }

                // Complete the message so that it is not received again.
                await _queue.DeleteMessageAsync(queueMessage);
                await AfterProcessedMessage(message, null);
            }
            catch (InvalidMessageException e)
            {
                // Invalid message exception isn't avoided, but it mustn't cause the report of yet another invalid message
                _logger.LogError($"{e.Message}\n{e.StackTrace}");
            }
            catch (DispatcherResetJump)
            {
                // Allows dispatcher to force cancel everything and return to the main listening routine
                // This means the message won't be acknowledged as read, so we reset the visibility
                await _queue.UpdateMessageAsync(queueMessage, TimeSpan.Zero, MessageUpdateFields.Visibility);
                throw;
            }
            catch (Exception e)
            {
                await ReportInvalidMessage(
                    queueMessage,
                    $"{e.GetType().Name} was thrown inside {GetType().Name} during message processing. " +
                    $"Exception message: {e.Message}\nStackTrace:\n{e.StackTrace}");
                throw new Exception("This never occurs");
            }
        }

        protected string MessagePairsToString(IDictionary<string, object> message)
        {
            var messagePairs = string.Join(
                Environment.NewLine, message.Select(pair =>
                    "  " + pair.Key + (pair.Value == null ? " null" : ": " + pair.Value.ToString())
                )
            );
            return $"{{\n{messagePairs}\n}}";
        }

        protected abstract Task Dispatch(CloudQueueMessage queueMessage, IDictionary<string, object> message);

        protected async Task<Exception> ReportInvalidMessage(CloudQueueMessage queueMessage, string errorMessage)
        {
            if (queueMessage == null)
            {
                _logger.LogError("Attempted to report invalid message, which was actually null.");
                throw new InvalidMessageException("Error during error report");
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
            await _deadLetterQueue.AddMessageAsync(deadLetterMessage);
            // Mark the message as completed, so that it is not received again
            await _queue.DeleteMessageAsync(queueMessage);

            throw new InvalidMessageException(
                $"Message handler couldn't handle a message with ID {queueMessage.Id}. "
                + $"Reported as a DeadLetter message ID {deadLetterMessage.Id}.",
                errorMessage
            );
        }

        protected async Task DeleteQueueAsync()
        {
            _queue = _queueClient.GetQueueReference(_queueName);
            await _queue.DeleteAsync();
            _logger.LogWarning($"Deleted queue {_queueName}");
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
            public string OriginalErrorMessage { get; }

            public InvalidMessageException(string description, string originalErrorMessage)
                : base(description + " Error was: " + originalErrorMessage)
            {
                OriginalErrorMessage = originalErrorMessage;
            }

            public InvalidMessageException(string errorMessage) : base(errorMessage)
            {
                OriginalErrorMessage = errorMessage;
            }
        }

        protected class DispatcherResetJump : Exception
        {
        }
    }
}
