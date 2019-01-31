using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Control;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.TradingService.Services
{
    /// <summary>
    /// Helps with persistence and usage of EventHistory Source-of-Truth Append-Only database.
    /// </summary>
    public class EventHistoryService
    {
        private EventHistoryRepository EventHistoryRepository { get; }
        private VersionControl VersionControl { get; }

        public EventHistoryService(EventHistoryRepository eventHistoryRepository, VersionControl versionControl)
        {
            EventHistoryRepository = eventHistoryRepository;
            VersionControl = versionControl;
        }

        /// <summary>
        /// Atomically persists multiple event entries representing a single version.
        /// </summary>
        public async Task<IList<EventEntry>> Persist(IEnumerable<EventEntry> eventTransaction)
        {
            // Copy the list before modifying it
            IList<EventEntry> events = new List<EventEntry>(eventTransaction);
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
            VersionControl.ExecuteUsingFixedVersion(currentDatabaseVersionNumber =>
            {
                // Make sure version number has not changed, as we can just save ourselves the useless effort otherwise
                if (currentDatabaseVersionNumber + 1 != versionNumber)
                {
                    versionNumberOutdatedAlready = true;
                    return;
                }

                // Attempt to atomically insert all entries
                EventHistoryRepository.Events().InsertMany(
                    events,
                    new InsertManyOptions {IsOrdered = true}
                );
            });
            if (versionNumberOutdatedAlready)
            {
                // Prematurely aborted insertion
                return null;
            }

            // Make sure it was inserted with the version number first without other same-versioned concurrent attempts
            var foundEventsCursor = await EventHistoryRepository.Events().FindAsync(
                EventHistoryRepository.VersionEqFilter(versionNumber)
            );
            var foundEvents = await foundEventsCursor.ToListAsync();
            IList<EventEntry> failedEntries = new List<EventEntry>();
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

            if (failedEntries.Count > 0)
            {
                await EventHistoryRepository.Events().DeleteManyAsync(e => failedEntries.Contains(e));
            }

            // Return null if the attempted transaction was not the first group of events with the same version number,
            // which means it was deemed invalid and then removed
            return thisSuccessful ? events : null;
        }

        public async Task<IList<EventEntry>> LoadMissingEvents(
            long currentVersionNumber,
            long? maxVersionNumber = null)
        {
            var allNewerEvents = (await EventHistoryRepository
                    .Events()
                    .FindAsync(EventHistoryRepository.VersionAboveFilter(currentVersionNumber, maxVersionNumber))
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
    }
}
