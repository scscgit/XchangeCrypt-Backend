using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;

namespace XchangeCrypt.Backend.WalletService.Providers
{
    public abstract class AbstractProvider : BackgroundService
    {
        public static readonly IDictionary<string, AbstractProvider> ProviderLookup =
            new Dictionary<string, AbstractProvider>();

        private readonly TimeSpan _listeningInterval = TimeSpan.FromMilliseconds(2000);
        private bool _stopped;
        protected readonly ILogger _logger;

        protected AbstractProvider(ILogger logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation(
                    $"Initialized {GetType().Name}, listening for event entries & blockchain events to be processed");
                while (!_stopped)
                {
                    ListenForBlockchainEvents().Wait(stoppingToken);

                    await Task.Delay(_listeningInterval, stoppingToken);
                    _logger.LogDebug($"{GetType().Name} is still listening for event entries & blockchain events...");
                }
            }
            catch (AggregateException e)
            {
                foreach (var innerException in e.InnerExceptions)
                {
                    _logger.LogError($"{innerException.Message}\n{innerException.StackTrace}");
                }

                Program.Shutdown();
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError($"{e.Message}\n{e.StackTrace}");
                Program.Shutdown();
                throw;
            }
        }

        protected abstract Task ListenForBlockchainEvents();

        public abstract void ProcessEvent(WalletEventEntry eventEntry);

        public abstract Task<string> GenerateHdWallet();

        public abstract Task<string> GetPublicKeyFromHdWallet(string hdSeed);

        public abstract Task<bool> Withdraw(
            string walletPublicKeyUserReference, string withdrawToPublicKey, decimal valueExclFee);

        public abstract Task PrepareWithdrawalAsync(
            WalletWithdrawalEventEntry withdrawalEventEntry, Action revocationAction);

        public abstract void OnDeposit(string fromPublicKey, string toPublicKey, decimal value);

        public abstract Task<decimal> GetBalance(string publicKey);

        public abstract Task<decimal> GetCurrentlyCachedBalance(string publicKey);

        public abstract Task<List<(string, decimal)>> GetWalletsHavingSumBalance(
            decimal sumBalance, string excludePublicKey, bool expectedSumAfterDeductingFees);

        public new async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopped = true;
            _logger.LogWarning($"Stopping {GetType().Name}");
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Expected fee to be paid by every single withdrawal, including consolidations.
        /// </summary>
        /// <returns></returns>
        public abstract decimal Fee();
    }
}
