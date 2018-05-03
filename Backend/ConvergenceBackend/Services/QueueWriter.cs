using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XchangeCrypt.Backend.ConvergenceBackend.Services
{
    /// <summary>
    /// Supports writing into a Service Bus Azure queue that prepares requests for all asynchronous actions
    /// to be executed in other backend components.
    /// </summary>
    public abstract class QueueWriter : IHostedService
    {
        private readonly string _serviceBusConnectionString;
        private readonly string _queueName;

        private IQueueClient _queueClient;

        /// <summary>
        /// </summary>
        public QueueWriter(string serviceBusConnectionString, string queueName)
        {
            _serviceBusConnectionString = serviceBusConnectionString;
            _queueName = queueName;
        }

        /// <summary>
        /// Initialization.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //_messageSender = new MessageSender(ServiceBusConnectionString, QueueName);
            _queueClient = new QueueClient(_serviceBusConnectionString, _queueName);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _queueClient.CloseAsync();
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
                var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                foreach (KeyValuePair<string, object> entry in userProperties)
                {
                    message.UserProperties.Add(entry.Key, entry.Value);
                }

                // Write the body of the message to the console
                Console.WriteLine($"Sending message: {messageBody}");

                // Send the message to the queue
                await _queueClient.SendAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {e.Message}");
                throw e;
            }
        }
    }

    /// <summary>
    /// TradingBackend instance of QueueWriter.
    /// </summary>
    public class TradingBackendQueueWriter : QueueWriter
    {
        // Connection String for the namespace can be obtained from the Azure portal under the
        // 'Shared Access policies' section.
        private const string ServiceBusConnectionString = "Endpoint=sb://xchangecrypttest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=//VIMVDa0Mi9zs0nPZGQvyk0yueSL4L8QOhfqF2Bd1k=";

        private const string QueueName = "TradeRequests";

        /// <summary>
        /// </summary>
        public TradingBackendQueueWriter(string serviceBusConnectionString, string queueName) : base(ServiceBusConnectionString, QueueName)
        {
        }
    }
}
