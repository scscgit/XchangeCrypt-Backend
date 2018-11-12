using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XchangeCrypt.Backend.ConvergenceBackend.Services
{
    /// <summary>
    /// Supports writing into a Service Bus Azure queue that prepares requests for all asynchronous actions
    /// to be executed in other backend components.
    /// </summary>
    public abstract class QueueWriter : IDisposable
    {
        private readonly IQueueClient _queueClient;

        /// <summary>
        /// </summary>
        protected QueueWriter(string serviceBusConnectionString, string queueName)
        {
            //_messageSender = new MessageSender(ServiceBusConnectionString, QueueName);
            _queueClient = new QueueClient(serviceBusConnectionString, queueName);
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
                var message = messageBody == null ? new Message() : new Message(Encoding.UTF8.GetBytes(messageBody));

                foreach (KeyValuePair<string, object> entry in userProperties)
                {
                    message.UserProperties.Add(entry.Key, entry.Value);
                }

                // Write the body of the message to the console
                Console.WriteLine($"Sending message with {userProperties.Count} properties: {messageBody ?? "no body"}");

                // Send the message to the queue
                await _queueClient.SendAsync(message);
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
            _queueClient.CloseAsync().Wait();
        }
    }
}
