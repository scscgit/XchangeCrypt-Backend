using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace XchangeCrypt.Backend.QueueAccess
{
    /// <summary>
    /// Supports writing into a Service Bus Azure queue that prepares requests for all asynchronous actions
    /// to be executed in other backend components.
    /// </summary>
    public class QueueWriter : IDisposable
    {
        private readonly ILogger<QueueWriter> _logger;
        private readonly CloudQueue _queue;

        public QueueWriter(string connectionString, string queueName, ILogger<QueueWriter> logger)
        {
            _logger = logger;
            //_messageSender = new MessageSender(ServiceBusConnectionString, QueueName);
            var storageAccount = CloudStorageAccount.Parse(
                connectionString ?? throw new ArgumentNullException(nameof(connectionString))
            );
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(
                queueName ?? throw new ArgumentNullException(nameof(queueName))
            );
            if (_queue.CreateIfNotExistsAsync().Result)
            {
                _logger.LogWarning($"Created queue {queueName}");
            }
        }

        /// <summary>
        /// Sends a message to the specified backend via queue.
        /// </summary>
        /// <param name="userProperties">Map of parameters of the queue</param>
        /// <param name="messageBody">Message to be delivered in the queue</param>
        public async Task SendMessageAsync(IDictionary<string, object> userProperties, string messageBody = null)
        {
            try
            {
                // Write the body of the message to the console
                _logger.LogInformation(
                    $"Sending message with {userProperties.Count} properties{(messageBody == null ? "" : ": " + messageBody)}");

                // Prepare the message
                userProperties.Add("MessageBody", messageBody);
                var message = new CloudQueueMessage(JsonConvert.SerializeObject(userProperties));

                // Send the message to the queue
                await _queue.AddMessageAsync(message, TimeSpan.FromSeconds(15), new TimeSpan?(), null, null);
            }
            catch (Exception e)
            {
                _logger.LogError($"{DateTime.Now} :: Exception: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
