using MongoDB.Bson;

namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class HotWallet
    {
        public ObjectId Id { get; set; }

        // TODO maybe unique ID
        public string CoinSymbol { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string HdSeed { get; set; }

        public string PublicKey { get; set; }
    }
}
