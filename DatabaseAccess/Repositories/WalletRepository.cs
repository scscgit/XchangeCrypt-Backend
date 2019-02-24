using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;

namespace XchangeCrypt.Backend.DatabaseAccess.Repositories
{
    public class WalletRepository
    {
        private IMongoDatabase Database { get; }

        public WalletRepository(DataAccess dataAccess)
        {
            Database = dataAccess.Database;
        }

        public IMongoCollection<HotWallet> HotWallets()
        {
            return Database.GetCollection<HotWallet>("HotWallets");
        }
    }
}
