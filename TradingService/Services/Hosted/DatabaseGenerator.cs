using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;
using XchangeCrypt.Backend.DatabaseAccess.Services;
using XchangeCrypt.Backend.TradingService.Processors.Event;

namespace XchangeCrypt.Backend.TradingService.Services.Hosted
{
    public class DatabaseGenerator : BackgroundService
    {
        private readonly TimeSpan _listeningInterval = TimeSpan.FromMilliseconds(2000);

        private readonly VersionControl _versionControl;
        private readonly EventHistoryService _eventHistoryService;
        private readonly TradingRepository _tradingRepository;
        private readonly AccountRepository _accountRepository;
        private readonly TradeEventProcessor _tradeEventProcessor;
        private readonly ILogger<DatabaseGenerator> _logger;

        private bool _stopped;
        private long _currentVersion;

        public DatabaseGenerator(
            VersionControl versionControl,
            EventHistoryService eventHistoryService,
            TradingRepository tradingRepository,
            AccountRepository accountRepository,
            TradeEventProcessor tradeEventProcessor,
            ILogger<DatabaseGenerator> logger)
        {
            _versionControl = versionControl;
            _eventHistoryService = eventHistoryService;
            _tradingRepository = tradingRepository;
            _accountRepository = accountRepository;
            _tradeEventProcessor = tradeEventProcessor;
            _logger = logger;
        }

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

        private void LoadDatabaseFromSnapshot()
        {
            // I lied, we won't load anything
            _currentVersion = 0;
            _tradingRepository.OrderBook().DeleteMany(
                Builders<OrderBookEntry>.Filter.Where(e => true)
            );
            _tradingRepository.HiddenOrders().DeleteMany(
                Builders<HiddenOrderEntry>.Filter.Where(e => true)
            );
            _tradingRepository.TransactionHistory().DeleteMany(
                Builders<TransactionHistoryEntry>.Filter.Where(e => true)
            );
            _tradingRepository.OrderHistory().DeleteMany(
                Builders<OrderHistoryEntry>.Filter.Where(e => true)
            );
            _accountRepository.Accounts().DeleteMany(
                new BsonDocumentFilterDefinition<AccountEntry>(new BsonDocument())
            );

            // This enables the version control semaphore
            _versionControl.Initialize(_currentVersion);
        }

        private async Task IntegrateNewEvents()
        {
            IList<EventEntry> missingEvents;
            try
            {
                missingEvents = await _eventHistoryService.LoadMissingEvents(_currentVersion);
            }
            catch (Exception)
            {
                _logger.LogError("Could not load missing events, the database is probably offline, will try later");
                return;
            }

            foreach (var eventEntry in missingEvents)
            {
                var eventVersion = eventEntry.VersionNumber;
                if (eventVersion != _currentVersion + 1)
                {
                    throw new Exception(
                        $"Integrity error: the event ID {eventEntry.Id} attempted to jump version from {_currentVersion} to {eventVersion}. This cannot be recovered from and requires a manual fix by administrator");
                }

                if (eventEntry is TransactionCommitEventEntry)
                {
                    _currentVersion = eventVersion;
                    _logger.LogInformation($"Integrated all events @ version number {_currentVersion.ToString()}");
                }
                else
                {
                    _logger.LogInformation(
                        $"Integrating {eventEntry.GetType().Name} @ version number {eventVersion.ToString()}");
                    _tradeEventProcessor.ProcessEvent((dynamic) eventEntry);
                }
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
