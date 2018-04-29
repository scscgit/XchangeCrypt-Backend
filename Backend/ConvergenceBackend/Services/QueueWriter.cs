using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ConvergenceBackend.Services
{
    public class QueueWriter
    {
        // Connection String for the namespace can be obtained from the Azure portal under the
        // 'Shared Access policies' section.
        private const string ServiceBusConnectionString = "Endpoint=sb://xchangecrypttest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=//VIMVDa0Mi9zs0nPZGQvyk0yueSL4L8QOhfqF2Bd1k=";

        private const string QueueName = "TradeRequests";

        private static IQueueClient queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
        //static MessageSender messageSender = new MessageSender(ServiceBusConnectionString, QueueName);

        public static async Task SendMessageAsync(IDictionary<string, object> userProperties, String messageBody)
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
                await queueClient.SendAsync(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }
    }
}
