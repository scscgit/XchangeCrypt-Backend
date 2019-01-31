using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.QueueAccess;

namespace XchangeCrypt.Backend.ConvergenceService.Services.Hosted
{
    public class AnswerQueueReceiver : AbstractDispatchReceiver
    {
        private class ExpectingAnswerPayload
        {
            public SemaphoreSlim Semaphore;
            public IDictionary<string, object> Message;
        }

        private readonly ILogger<AbstractDispatchReceiver> _logger;

        private readonly IDictionary<string, ExpectingAnswerPayload> _expectingAnswers =
            new Dictionary<string, ExpectingAnswerPayload>();

        public AnswerQueueReceiver(
            IConfiguration configuration,
            ILogger<AbstractDispatchReceiver> logger)
            : base(
                configuration["Queue:ConnectionString"],
                configuration["Queue:Answer:NamePrefix"],
                configuration["Queue:Answer:DeadLetterName"],
                Program.Shutdown,
                logger)
        {
            _logger = logger;
        }

        protected override QueueWriter ConfigureQueueAnswerWriter(Func<string> answerQueuePostfixRequest)
        {
            // This receiver only consumes, it never replies
            return null;
        }

        protected override Task Dispatch(CloudQueueMessage queueMessage, IDictionary<string, object> message)
        {
            var user = (string) message[MessagingConstants.ParameterNames.User];
            var requestId = (string) message[MessagingConstants.ParameterNames.RequestId];
            if (!_expectingAnswers.ContainsKey(user + " " + requestId))
            {
                _logger.LogWarning("Detected a conflicting entry in the answer queue! Changing queue postfix");
                throw new DispatcherResetJump();
            }

            var payload = _expectingAnswers[user + " " + requestId];
            payload.Message = message;
            // Wait until the message is properly consumed (twice, so that the dictionary is also cleaned up)
            payload.Semaphore.Release(2);
            return Task.CompletedTask;
        }

        protected override void DispatcherPostfixReset(out string queueNamePostfix)
        {
            // Generating a new random queue postfix, so that no message answer conflicts will occur
            // (Note that pending answers may be lost at the immediate moment after convergence service is scaled up)
            queueNamePostfix = new Random().Next(50000).ToString();
        }

        public void ExpectAnswer(string user, string requestId)
        {
            // Asserting that user and requestId never contain space, and they are both reasonably bounded
            _expectingAnswers[user + " " + requestId] = new ExpectingAnswerPayload
            {
                Semaphore = new SemaphoreSlim(0, 2)
            };
        }

        public async Task<IDictionary<string, object>> WaitForAnswer(string user, string requestId, TimeSpan timeout)
        {
            // Asserting that user and requestId never contain space, and they are both reasonably bounded
            var payload = _expectingAnswers[user + " " + requestId];
            await payload.Semaphore.WaitAsync(timeout);
            // After waiting on the semaphore, there is a message available, but we clean up the request first
            _expectingAnswers.Remove(user + " " + requestId);
            await payload.Semaphore.WaitAsync(TimeSpan.Zero);
            if (payload.Message == null)
            {
                _logger.LogWarning($"No matching answer in the answer queue for request id {requestId}");
                return new Dictionary<string, object>
                {
                    {MessagingConstants.ParameterNames.ErrorIfAny, "No answer was received from the trading service"}
                };
            }

            _logger.LogInformation(
                $"Received matching answer in the answer queue: {MessagePairsToString(payload.Message)}");
            return payload.Message;
        }

        public new async Task DeleteQueueAsync()
        {
            await base.DeleteQueueAsync();
        }
    }
}
