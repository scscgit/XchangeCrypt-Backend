using MongoDB.Driver;
using XchangeCrypt.Backend.TradingBackend.Models;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;

namespace XchangeCrypt.Backend.TradingBackend.Repositories
{
    public class ActivityHistoryRepository
    {
        public IMongoDatabase Database { get; }

        public ActivityHistoryRepository(DataAccess dataAccess)
        {
            Database = dataAccess.Database;
        }

        public IMongoCollection<ActivityHistoryOrderEntry> Orders()
        {
            return Database.GetCollection<ActivityHistoryOrderEntry>("ActivityHistory");
        }

        public IMongoCollection<ActivityHistoryWalletOperationEntry> WalletOperations()
        {
            return Database.GetCollection<ActivityHistoryWalletOperationEntry>("ActivityHistory");
        }

        public static FilterDefinition<ActivityHistoryOrderEntry> OrdersFilter
        {
            get
            {
                return Builders<ActivityHistoryOrderEntry>.Filter.Where(e =>
                    e.EntryType == ActivityHistoryEntryType.TradeOrder);
            }
        }

        public static FilterDefinition<ActivityHistoryWalletOperationEntry> WalletOperationsFilter
        {
            get
            {
                return Builders<ActivityHistoryWalletOperationEntry>.Filter.Where(e =>
                    e.EntryType == ActivityHistoryEntryType.WalletOperation);
            }
        }
    }
}
