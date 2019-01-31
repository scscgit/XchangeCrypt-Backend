using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models.Events;

namespace XchangeCrypt.Backend.DatabaseAccess.Repositories
{
    public class EventHistoryRepository
    {
        private IMongoDatabase Database { get; }

        public EventHistoryRepository(DataAccess dataAccess)
        {
            Database = dataAccess.Database;
        }

        public IMongoCollection<EventEntry> Events()
        {
            return Events<EventEntry>();
        }

        public IMongoCollection<T> Events<T>() where T : EventEntry
        {
            return Database.GetCollection<T>("EventHistory");
        }

        public static FilterDefinition<EventEntry> VersionEqFilter(long versionNumber)
        {
            return VersionEqFilter<EventEntry>(versionNumber);
        }

        public static FilterDefinition<T> VersionEqFilter<T>(long versionNumber) where T : EventEntry
        {
            return Builders<T>.Filter.Eq(e => e.VersionNumber, versionNumber);
        }

        public static FilterDefinition<EventEntry> VersionAboveFilter(long aboveVersionNumber,
            long? maxVersionNumber = null)
        {
            return VersionAboveFilter<EventEntry>(aboveVersionNumber, maxVersionNumber);
        }

        public static FilterDefinition<T> VersionAboveFilter<T>(long aboveVersionNumber, long? maxVersionNumber = null)
            where T : EventEntry
        {
            var gt = Builders<T>.Filter.Gt(e => e.VersionNumber, aboveVersionNumber);
            return maxVersionNumber.HasValue
                ? Builders<T>.Filter.And(gt, Builders<T>.Filter.Lte(e => e.VersionNumber, maxVersionNumber.Value))
                : gt;
        }
    }
}
