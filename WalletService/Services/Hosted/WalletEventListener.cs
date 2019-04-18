using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.WalletService.Providers;

namespace XchangeCrypt.Backend.WalletService.Services.Hosted
{
    public class WalletEventListener : BackgroundService
    {
        private int _listeningInterval = 2000;
        private readonly int _listeningIntervalMax = 2000;

        private long _currentVersion = 0;
        private readonly EventHistoryService _eventHistoryService;
        private readonly VersionControl _versionControl;
        private readonly ILogger<WalletEventListener> _logger;
        private bool _stopped;

        public WalletEventListener(
            EventHistoryService eventHistoryService,
            VersionControl versionControl,
            ILogger<WalletEventListener> logger)
        {
            _eventHistoryService = eventHistoryService;
            _versionControl = versionControl;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // This enables the version control semaphore
                _versionControl.Initialize(_currentVersion);
                _logger.LogInformation(
                    $"Initialized {GetType().Name}, listening for wallet event entries to be processed");
                while (!_stopped)
                {
                    // We can batch events by using a second parameter, just make sure to consume the entire version
                    var missingEvents = await _eventHistoryService.LoadMissingEvents(_currentVersion);
                    if (missingEvents.Count > 0)
                    {
                        // Only integrate new events as long as no one is currently assuming a fixed current version
                        _versionControl.IncreaseVersion(() =>
                        {
                            foreach (var missingEvent in missingEvents)
                            {
                                if (missingEvent is WalletEventEntry walletEvent)
                                {
                                    AbstractProvider.ProviderLookup[walletEvent.CoinSymbol].ProcessEvent(walletEvent);
                                }

                                var eventVersionNumber = missingEvent.VersionNumber;
                                if (eventVersionNumber == _currentVersion)
                                {
                                    continue;
                                }

                                if (eventVersionNumber == _currentVersion + 1)
                                {
                                    _currentVersion++;
                                    continue;
                                }

                                throw new Exception(
                                    $"Integrity error: the event ID {missingEvent.Id} attempted to jump version from {_currentVersion} to {eventVersionNumber}. This cannot be recovered from and requires a manual fix by administrator");
                            }

                            _logger.LogInformation($"Current version is increased to {_currentVersion}");
                            return _currentVersion;
                        });
                        _listeningInterval = 50;
                    }
                    else
                    {
                        if (_listeningInterval < _listeningIntervalMax)
                        {
                            _listeningInterval += 50;
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(_listeningInterval), stoppingToken);
                        _logger.LogDebug($"{GetType().Name} is still listening for event entries...");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}\n{e.StackTrace}");
                Program.Shutdown();
                throw;
            }
        }

        public new async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopped = true;
            _logger.LogWarning("Stopping database generator");
            await base.StopAsync(cancellationToken);
        }
    }
}
