using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;

namespace XchangeCrypt.Backend.DatabaseAccess.Repositories
{
    public class AccountRepository
    {
        public IMongoDatabase Database { get; }

        public AccountRepository(DataAccess dataAccess)
        {
            Database = dataAccess.Database;
        }

        public IMongoCollection<AccountEntry> Orders()
        {
            return Database.GetCollection<AccountEntry>("Accounts");
        }
    }
}
