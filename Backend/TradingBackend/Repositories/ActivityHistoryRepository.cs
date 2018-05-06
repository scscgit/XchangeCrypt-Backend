using MongoDB.Driver;
using XchangeCrypt.Backend.TradingBackend.Models;

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
    }
}
