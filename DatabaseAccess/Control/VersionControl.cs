using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace XchangeCrypt.Backend.DatabaseAccess.Control
{
    public class VersionControl
    {
        private readonly ILogger<VersionControl> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        public long CurrentVersion { get; private set; }

        public VersionControl(ILogger<VersionControl> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Call only once to unlock the semaphore.
        /// </summary>
        /// <param name="currentVersion">initial current version</param>
        public void Initialize(long currentVersion)
        {
            CurrentVersion = currentVersion;
            _semaphore.Release();
        }

        public void ExecuteUsingFixedVersion(Action<long> actionVersionNumber)
        {
            while (!_semaphore.Wait(TimeSpan.FromSeconds(10)))
            {
                _logger.LogWarning(
                    $"{nameof(ExecuteUsingFixedVersion)} couldn't acquire semaphore after 10 seconds, retrying...");
            }

            try
            {
                actionVersionNumber(CurrentVersion);
            }
            catch (AggregateException e)
            {
                foreach (var innerException in e.InnerExceptions)
                {
                    _logger.LogError($"{innerException.Message}\n{innerException.StackTrace}");
                }

                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void IncreaseVersion(Func<long> actionGetVersionNumber)
        {
            while (!_semaphore.Wait(TimeSpan.FromSeconds(10)))
            {
                _logger.LogWarning(
                    $"{nameof(IncreaseVersion)} couldn't acquire semaphore after 10 seconds, retrying...");
            }

            try
            {
                CurrentVersion = actionGetVersionNumber();
            }
            catch (AggregateException e)
            {
                foreach (var innerException in e.InnerExceptions)
                {
                    _logger.LogError($"{innerException.Message}\n{innerException.StackTrace}");
                }

                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void WaitForIntegration(long versionNumber)
        {
            var retry = true;
            while (retry)
            {
                if (CurrentVersion >= versionNumber)
                {
                    return;
                }

                Task.Delay(250).Wait();
                ExecuteUsingFixedVersion(currentVersion =>
                {
                    if (currentVersion >= versionNumber)
                    {
                        retry = false;
                    }
                });
            }
        }
    }
}
