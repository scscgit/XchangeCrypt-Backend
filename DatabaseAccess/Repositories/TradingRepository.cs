using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;

namespace XchangeCrypt.Backend.DatabaseAccess.Repositories
{
    public class TradingRepository
    {
        public IMongoDatabase Database { get; }

        public TradingRepository(DataAccess dataAccess)
        {
            Database = dataAccess.Database;
        }

        public IMongoCollection<OrderBookEntry> OrderBook()
        {
            return Database.GetCollection<OrderBookEntry>("OrderBook");
        }

        public IMongoCollection<HiddenOrderEntry> HiddenOrders()
        {
            return Database.GetCollection<HiddenOrderEntry>("HiddenOrders");
        }

        public IMongoCollection<TransactionHistoryEntry> TransactionHistory()
        {
            return Database.GetCollection<TransactionHistoryEntry>("TransactionHistory");
        }

        public IMongoCollection<OrderHistoryEntry> OrderHistory()
        {
            return Database.GetCollection<OrderHistoryEntry>("OrderHistory");
        }
    }
}
