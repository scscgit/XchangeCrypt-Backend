namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class HotWallet
    {
        // TODO maybe unique ID
        public string CoinSymbol;

        public string User { get; set; }

        public string AccountId { get; set; }

        public string HdSeed;

        public string PrivateKey;

        public string PublicKey;
    }
}
