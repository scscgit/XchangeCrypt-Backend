using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Services;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace XchangeCrypt.Backend.TradingBackend.Dispatch
{
    public class DispatchReceiver : IHostedService
    {
        // Connection String for the namespace can be obtained from the Azure portal under the
        // 'Shared Access policies' section.
        private const string ServiceBusConnectionString = "Endpoint=sb://xchangecrypttest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=//VIMVDa0Mi9zs0nPZGQvyk0yueSL4L8QOhfqF2Bd1k=";

        private const string QueueName = "TradeRequests";

        private readonly MonitorService _monitorService;
        private readonly TradeOrderDispatch _tradingOrderDispatch;
        private IQueueClient _queueClient;

        public DispatchReceiver(MonitorService monitorService, TradeOrderDispatch tradingOrderDispatch)
        {
            _monitorService = monitorService;
            _tradingOrderDispatch = tradingOrderDispatch;
        }

        /// <summary>
        /// Handler registration.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _queueClient = new QueueClient(ServiceBusConnectionString, QueueName, ReceiveMode.PeekLock);
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
                var messageBody = message.Body == null ? "" : Encoding.UTF8.GetString(message.Body);
                Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{messageBody}");

                Task dispatchedTask;
                switch (message.UserProperties[ParameterNames.MessageType])
                {
                    case MessageTypes.TradeOrder:
                        dispatchedTask = _tradingOrderDispatch.Dispatch(message, errorMessage => ReportInvalidMessage(message, errorMessage));
                        break;

                    case MessageTypes.WalletOperation:
                        dispatchedTask = _tradingOrderDispatch.Dispatch(message, errorMessage => ReportInvalidMessage(message, errorMessage));
                        break;

                    default:
                        await ReportInvalidMessage(message, $"Unrecognized MessageType {message.UserProperties[ParameterNames.MessageType]}");
                        return;
                }
                // Throws invalid message exception on error
                await dispatchedTask;

                // Complete the message so that it is not received again.
                // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
                await _queueClient.CompleteAsync(message.SystemProperties.LockToken);
                var messageDescription = $"{message.UserProperties[ParameterNames.MessageType]} ID {message.MessageId}, body: {messageBody}";
                Console.WriteLine($"Consumed message {messageDescription}");
                _monitorService.LastMessage = messageDescription;

                // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
                // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls
                // to avoid unnecessary exceptions.
            }
            catch (InvalidMessageException e)
            {
                // Invalid message exception isn't avoided, but it mustn't cause the report of yet another invalid message
                throw e;
            }
            catch (Exception e)
            {
                await ReportInvalidMessage(
                    message,
                    $"{e.GetType().Name} was thrown inside {typeof(DispatchReceiver).Name} during message processing. " +
                    $"Exception message: {e.Message}. StackTrace: {e.StackTrace}");
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
            throw new InvalidMessageException(error);
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

        public class InvalidMessageException : Exception
        {
            public InvalidMessageException(string message) : base(message)
            {
            }
        }
    }
}