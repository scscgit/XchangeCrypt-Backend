using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace XchangeCrypt.Backend.ConvergenceBackend.Services
{
    /// <summary>
    /// Supports writing into a Service Bus Azure queue that prepares requests for all asynchronous actions
    /// to be executed in other backend components.
    /// </summary>
    public abstract class QueueWriter : IDisposable
    {
        private readonly CloudQueue _queue;

        /// <summary>
        /// </summary>
        protected QueueWriter(string serviceBusConnectionString, string queueName)
        {
            //_messageSender = new MessageSender(ServiceBusConnectionString, QueueName);
            var storageAccount = CloudStorageAccount.Parse(serviceBusConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(queueName);
            if (_queue.CreateIfNotExistsAsync().Result)
            {
                Console.WriteLine($"Created queue {queueName}");
            }
        }

        /// <summary>
        /// Sends a message to the specified backend via queue.
        /// </summary>
        /// <param name="userProperties">Map of parameters of the queue</param>
        /// <param name="messageBody">Message to be delivered in the queue</param>
        public async Task SendMessageAsync(IDictionary<string, object> userProperties, String messageBody = null)
        {
            try
            {
                // Write the body of the message to the console
                Console.WriteLine(
                    $"Sending message with {userProperties.Count} properties: {messageBody ?? "no body"}");

                // Prepare the message
                userProperties.Add("MessageBody", messageBody);
                var message = new CloudQueueMessage(JsonConvert.SerializeObject(userProperties));

                // Send the message to the queue
                await _queue.AddMessageAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {e.Message}");
                throw e;
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
