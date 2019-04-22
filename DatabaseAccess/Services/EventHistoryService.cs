using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.DatabaseAccess.Services
{
    /// <summary>
    /// Helps with persistence and usage of EventHistory Source-of-Truth Append-Only database.
    /// </summary>
    public class EventHistoryService
    {
        private readonly ILogger<EventHistoryService> _logger;
        private EventHistoryRepository EventHistoryRepository { get; }
        public VersionControl VersionControl { get; }

        public EventHistoryService(
            EventHistoryRepository eventHistoryRepository,
            VersionControl versionControl,
            ILogger<EventHistoryService> logger)
        {
            _logger = logger;
            EventHistoryRepository = eventHistoryRepository;
            VersionControl = versionControl;
        }

        /// <summary>
        /// Atomically persists multiple event entries representing a single version.
        /// </summary>
        public virtual async Task<IList<EventEntry>> Persist(
            IEnumerable<EventEntry> eventTransaction,
            long? alreadyLockedVersionNumber = null)
        {
            // Copy the list before modifying it
            var events = new List<EventEntry>(eventTransaction);
            // Take the version number, they all use the same
            var versionNumber = events[0].VersionNumber;
            // Add the commit event at the end in order to prevent mixing multiple transactions
            var commit = new TransactionCommitEventEntry
            {
                VersionNumber = versionNumber
            };
            events.Add(commit);
            // Double-check validate them all to have the same version number, and assign the same current time
            var now = CurrentTime();
            foreach (var eventEntry in events)
            {
                if (eventEntry.VersionNumber != versionNumber)
                {
                    throw new Exception(
                        $"Integrity error, attempted to persist a transaction consisting of events having different version numbers: expected {versionNumber.ToString()}, actual {eventEntry.VersionNumber.ToString()}");
                }

                eventEntry.EntryTime = now;
            }

            // Take the semaphore, so that no other action can use the current version as it's about to change
            var versionNumberOutdatedAlready = false;

            void InsertLambda(long currentDatabaseVersionNumber)
            {
                // Make sure version number has not changed, as we can just save ourselves the useless effort otherwise
                if (currentDatabaseVersionNumber + 1 != versionNumber)
                {
                    versionNumberOutdatedAlready = true;
                    return;
                }

                // We are under synchronization, so we can double-check that we are ahead against other services
                if (EventHistoryRepository.Events()
                        .Find(e => e.VersionNumber > currentDatabaseVersionNumber)
                        .CountDocuments() != 0)
                {
                    versionNumberOutdatedAlready = true;
                    return;
                }

                // Attempt to atomically insert all entries
                EventHistoryRepository.Events().InsertMany(events, new InsertManyOptions {IsOrdered = true});
            }

            if (alreadyLockedVersionNumber.HasValue)
            {
                InsertLambda(alreadyLockedVersionNumber.Value);
            }
            else
            {
                VersionControl.ExecuteUsingFixedVersion(InsertLambda);
            }

            if (versionNumberOutdatedAlready)
            {
                // Prematurely aborted insertion
                _logger.LogError($"Reason for event @ version number {versionNumber} retry: already outdated");
                return null;
            }

            // Make sure it was inserted with the version number first without other same-versioned concurrent attempts
            var foundEventsCursor = await EventHistoryRepository.Events().FindAsync(
                EventHistoryRepository.VersionEqFilter(versionNumber)
            );
            var foundEvents = await foundEventsCursor.ToListAsync();
            var failedEntries = new List<EventEntry>();
            var foundCommit = false;
            var thisSuccessful = false;
            foreach (var foundEvent in foundEvents)
            {
                if (foundCommit)
                {
                    failedEntries.Add(foundEvent);
                }
                else if (foundEvent is TransactionCommitEventEntry)
                {
                    foundCommit = true;
                    thisSuccessful = foundEvent.Id.Equals(commit.Id);
                }
            }

            // A nasty workaround to clean up invalid events. They won't be processed, so it's not vital for operation.
            // This is not guaranteed to execute though, so TODO change or make another cleanup!
            foreach (var failedEntry in failedEntries)
            {
                _logger.LogError(
                    $"Note: removing duplicate (uncommitted) failed event entry {failedEntry.GetType().Name} @ version number {versionNumber}");
                // Re-written to be sequential, as there were issues with DeleteMany LINQ selector
                await EventHistoryRepository.Events().DeleteOneAsync(
                    e => failedEntry.Id.Equals(e.Id)
                );
            }

            // Return null if the attempted transaction was not the first group of events with the same version number,
            // which means it was deemed invalid and then removed
            return thisSuccessful ? events : null;
        }

        protected virtual DateTime CurrentTime()
        {
            return DateTime.Now;
        }

        public virtual async Task<IList<EventEntry>> LoadMissingEvents(
            long currentVersionNumber,
            long? maxVersionNumber = null)
        {
            // TODO DELETE ME: fast database purge for development purposes
            //EventHistoryRepository.Events().DeleteMany(Builders<EventEntry>.Filter.Gt(e => e.VersionNumber, INSERT_NUMBER));

            var allNewerEvents = (await EventHistoryRepository
                    .Events()
                    .FindAsync(
                        EventHistoryRepository.VersionAboveFilter(currentVersionNumber, maxVersionNumber)
//                        ,new FindOptions<EventEntry>
//                        {
//                            // The events need to be deterministically sorted by their version number!
//                            Sort = Builders<EventEntry>.Sort.Ascending(e => e.VersionNumber)
//                        }
                    )
                ).ToList();

            var validEvents = allNewerEvents
                .GroupBy(e => e.VersionNumber)
                .SelectMany(version =>
                {
                    var validEventsOnVersion = new List<EventEntry>();
                    foreach (var eventEntry in version)
                    {
                        validEventsOnVersion.Add(eventEntry);
                        if (eventEntry is TransactionCommitEventEntry)
                        {
                            // Commit found, consider other events invalid
                            break;
                        }
                    }

                    return validEventsOnVersion;
                })
                .ToList();
            return validEvents;
        }

        public EventEntry FindById(ObjectId eventId)
        {
            return EventHistoryRepository.Events().Find(eventEntry => eventEntry.Id.Equals(eventId)).Single();
        }

        public WalletGenerateEventEntry FindWalletGenerateByPublicKey(string publicKey)
        {
            return (WalletGenerateEventEntry) EventHistoryRepository.Events().Find(eventEntry =>
                eventEntry is WalletGenerateEventEntry
                && ((WalletGenerateEventEntry) eventEntry).LastWalletPublicKey.Equals(publicKey)).Single();
        }

        public void ReportOverdrawnWithdrawal(WalletWithdrawalEventEntry withdrawal)
        {
            EventHistoryRepository.Events().FindOneAndUpdate(
                eventEntry => eventEntry.Id.Equals(withdrawal.Id),
                Builders<EventEntry>.Update.Set(
                    eventEntry => ((WalletWithdrawalEventEntry) eventEntry).OverdrawnAndCanceledOrders,
                    true
                )
            );
        }

        public void ReportWithdrawalExecuted(WalletWithdrawalEventEntry withdrawal, Action afterMarkedExecuted)
        {
            // Note: this must occur after withdrawal event processing by this own service
            // TODO: try to use VersionControl.WaitForIntegration instead (same functionality)
            var retry = true;
            while (retry)
            {
                VersionControl.ExecuteUsingFixedVersion(currentVersion =>
                {
                    if (currentVersion < withdrawal.VersionNumber)
                    {
                        return;
                    }

                    EventHistoryRepository.Events().FindOneAndUpdate(
                        eventEntry => eventEntry.Id.Equals(withdrawal.Id),
                        Builders<EventEntry>.Update.Set(
                            eventEntry => ((WalletWithdrawalEventEntry) eventEntry).Executed,
                            true
                        )
                    );
                    afterMarkedExecuted();
                    _logger.LogInformation(
                        $"Reported executed withdrawal of {withdrawal.WithdrawalQty} {withdrawal.CoinSymbol}");
                    retry = false;
                });
                if (retry)
                {
                    _logger.LogInformation(
                        $"{nameof(ReportWithdrawalExecuted)} waiting for integration of version number {withdrawal.VersionNumber}");
                    Task.Delay(1000).Wait();
                }
            }
        }

        public void ReportWithdrawalValidation(WalletWithdrawalEventEntry withdrawal, bool validation)
        {
            _logger.LogInformation(
                $"Validation of {withdrawal.WithdrawalQty} {withdrawal.CoinSymbol} withdrawal {(validation ? "successful" : "failed")}");
            EventHistoryRepository.Events().FindOneAndUpdate(
                eventEntry => eventEntry.Id.Equals(withdrawal.Id),
                Builders<EventEntry>.Update.Set(
                    eventEntry => ((WalletWithdrawalEventEntry) eventEntry).Validated,
                    validation
                )
            );
        }

        public void ReportConsolidationExecuted(WalletConsolidationTransferEventEntry consolidation)
        {
            _logger.LogInformation(
                $"Reported a new executed consolidation of {consolidation.TransferQty} {consolidation.CoinSymbol}, target becomes {consolidation.NewTargetPublicKeyBalance}");
            EventHistoryRepository.Events().FindOneAndUpdate(
                eventEntry => eventEntry.Id.Equals(consolidation.Id),
                Builders<EventEntry>.Update.Set(
                    eventEntry => ((WalletConsolidationTransferEventEntry) eventEntry).Executed,
                    true
                )
            );
        }

        public void ReportConsolidationValidated(WalletConsolidationTransferEventEntry consolidation, bool validation)
        {
            _logger.LogInformation(
                $"Validation of {consolidation.TransferQty} {consolidation.CoinSymbol} consolidation {(validation ? "successful" : "failed")}");
            EventHistoryRepository.Events().FindOneAndUpdate(
                eventEntry => eventEntry.Id.Equals(consolidation.Id),
                Builders<EventEntry>.Update.Set(
                    eventEntry => ((WalletConsolidationTransferEventEntry) eventEntry).Valid,
                    validation
                )
            );
        }

        public List<EventEntry> FindByVersionNumber(long versionNumber)
        {
            return EventHistoryRepository.Events()
                .Find(eventEntry => eventEntry.VersionNumber.Equals(versionNumber))
                .ToList();
        }
    }
}
