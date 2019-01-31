using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace XchangeCrypt.Backend.DatabaseAccess.Control
{
    public class VersionControl
    {
        private readonly ILogger<VersionControl> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private long _currentVersion;

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
            _currentVersion = currentVersion;
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
                actionVersionNumber(_currentVersion);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void IncreaseVersion(Func<long> actionVersionNumber)
        {
            while (!_semaphore.Wait(TimeSpan.FromSeconds(10)))
            {
                _logger.LogWarning(
                    $"{nameof(IncreaseVersion)} couldn't acquire semaphore after 10 seconds, retrying...");
            }

            try
            {
                _currentVersion = actionVersionNumber();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
