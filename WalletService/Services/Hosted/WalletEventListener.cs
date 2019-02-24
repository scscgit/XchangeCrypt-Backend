using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using XchangeCrypt.Backend.WalletService.Providers;

namespace XchangeCrypt.Backend.WalletService.Services.Hosted
{
    public class WalletEventListener: BackgroundService
    {
        public WalletEventListener
        
        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                LoadDatabaseFromSnapshot();
                _logger.LogInformation($"Initialized {GetType().Name}, listening for event entries to be processed");
                while (!_stopped)
                {
                    // Only integrate new events as long as no one is currently assuming a fixed current version
                    _versionControl.IncreaseVersion(() =>
                    {
                        IntegrateNewEvents().Wait(stoppingToken);
                        return _currentVersion;
                    });

                    await Task.Delay(_listeningInterval, stoppingToken);
                    _logger.LogDebug($"{GetType().Name} is still listening for event entries...");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}\n{e.StackTrace}");
                Program.Shutdown();
                throw;
            }
        }
    }
}
