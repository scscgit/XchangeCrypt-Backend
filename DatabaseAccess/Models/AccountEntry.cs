using System.Collections.Generic;
using MongoDB.Bson;

namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class AccountEntry
    {
        public ObjectId Id { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public IList<CoinWallet> CoinWallets { get; set; }
    }
}
