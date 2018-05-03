using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Services;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend
{
    public class TradeDispatchReceiver : IHostedService
    {
        // Connection String for the namespace can be obtained from the Azure portal under the
        // 'Shared Access policies' section.
        private const string ServiceBusConnectionString = "Endpoint=sb://xchangecrypttest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=//VIMVDa0Mi9zs0nPZGQvyk0yueSL4L8QOhfqF2Bd1k=";

        private const string QueueName = "TradeRequests";

        private MonitorService _monitorService;
        private LimitOrderService _limitOrderService;
        private IQueueClient _queueClient;

        public TradeDispatchReceiver(MonitorService monitorService, LimitOrderService limitOrderService)
        {
            _monitorService = monitorService;
            _limitOrderService = limitOrderService;

            _queueClient = new QueueClient(ServiceBusConnectionString, QueueName, ReceiveMode.PeekLock);
        }

        /// <summary>
        /// Handler registration.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            RegisterReceiveMessagesHandler();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _queueClient.CloseAsync();
        }

        private void RegisterReceiveMessagesHandler()
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false
            };

            // Register the function that will process messages
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            try
            {
                // Process the message
                Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

                switch (message.UserProperties[ParameterNames.MessageType])
                {
                    case MessageTypes.LimitOrder:
                        switch (message.UserProperties[ParameterNames.Side])
                        {
                            case "buy":
                                _limitOrderService.Buy(
                                    (string)message.UserProperties["user"],
                                    (int)message.UserProperties["limitPrice"]
                                    );
                                break;

                            case "sell":
                                _limitOrderService.Sell(
                                    (string)message.UserProperties["user"],
                                    (int)message.UserProperties["limitPrice"]
                                    );
                                break;

                            default:
                                await ReportInvalidMessage(message, $"Not recognized LimitOrder message side {message.UserProperties["Side"]}");
                                return;
                        }
                        break;

                    default:
                        await ReportInvalidMessage(message, $"Not recognized message type {message.UserProperties["MessageType"]}");
                        return;
                }

                // Complete the message so that it is not received again.
                // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
                await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
                var messageDescription = $"{message.UserProperties[ParameterNames.MessageType]} ID {message.MessageId}, body: {message.Body}";
                Console.WriteLine($"Consumed message {messageDescription}");
                _monitorService.LastMessage = messageDescription;

                // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
                // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls
                // to avoid unnecessary exceptions.
            }
            catch (Exception e)
            {
                await ReportInvalidMessage(message, $"{e.GetType().Name} was thrown inside {typeof(TradeDispatchReceiver).Name} during message processing. Exception message: {e.Message}");
            }
        }

        private async Task ReportInvalidMessage(Message message, string errorMessage)
        {
            var deadLetterTask = _queueClient.DeadLetterAsync(message.SystemProperties.LockToken, new Dictionary<string, object>() { { "errorMessage", errorMessage } });

            var error = $"Message handler couldn't handle a message with ID {message.MessageId}.\n";
            error += $"{errorMessage}\n";
            Console.Write(error);
            _monitorService.ReportError(error);
            await deadLetterTask;
        }

        // Use this Handler to look at the exceptions received on the MessagePump
        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var error = $"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.\n";
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            error += "Exception context for troubleshooting:\n";
            error += $"- Endpoint: {context.Endpoint}\n";
            error += $"- Entity Path: {context.EntityPath}\n";
            error += $"- Executing Action: {context.Action}\n";
            Console.Write(error);
            _monitorService.ReportError(error);
            return Task.CompletedTask;
        }
    }
}
