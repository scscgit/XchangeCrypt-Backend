using MongoDB.Bson;
using System.Collections.Generic;

namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class AccountEntry
    {
        public ObjectId Id { get; set; }

        public string AccountId { get; set; }

        public IList<CoinWallet> Coins { get; set; }
    }
}
