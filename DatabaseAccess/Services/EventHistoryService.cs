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
        private VersionControl VersionControl { get; }

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
        public async Task<IList<EventEntry>> Persist(
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
            var now = DateTime.Now;
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

            // A nasty workaround to clean up invalid events. This is not guaranteed to execute though, so TODO!
            foreach (var failedEntry in failedEntries)
            {
                _logger.LogError(
                    $"Note: removing duplicate (uncommitted) failed event entry @ version number {versionNumber}");
                // Re-written to be sequential, as there were issues with DeleteMany LINQ selector
                await EventHistoryRepository.Events().DeleteOneAsync(
                    e => failedEntry.Id.Equals(e.Id)
                );
            }

            // Return null if the attempted transaction was not the first group of events with the same version number,
            // which means it was deemed invalid and then removed
            return thisSuccessful ? events : null;
        }

        public async Task<IList<EventEntry>> LoadMissingEvents(
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
    }
}
